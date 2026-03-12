# UMS — Universal AI Development Context
> Paste this file at the start of any AI session. The AI will have full context to code immediately.

---

## 1. PROJECT IDENTITY

| Property | Value |
|---|---|
| Solution | `AspireApp1` — University Management System (UMS) |
| Repo | `C:\Users\harsh\source\repos\AspireApp1` |
| Drop folder | `C:\Users\harsh\Downloads\ums-drop` |
| Framework | .NET 10 + .NET Aspire 9.3.1 |
| Runtime | `net10.0` across all projects |
| Shell | PowerShell (Windows 11) |
| IDE | Visual Studio 2022 |
| Package management | Central Package Management via `Directory.Packages.props` |
| Container runtime | Docker + Minikube (namespace: `ums`) |
| Orchestration | Kubernetes with Kustomize (`k8s/base` + `k8s/overlays/dev-local`) |

---

## 2. SOLUTION STRUCTURE

```
AspireApp1/
├── Directory.Packages.props          # ALL NuGet versions — never add Version= to .csproj
├── src/
│   ├── AppHost/                      # .NET Aspire orchestrator — registers all services
│   ├── ServiceDefaults/              # Shared: OTel, Serilog, health checks, resilience
│   ├── Shared/UMS.SharedKernel/      # Domain primitives only (see Section 5)
│   ├── ApiGateway/                   # YARP reverse proxy + JWT validation
│   ├── BFF/                          # Backend-for-Frontend (HTTP aggregation)
│   ├── Web/                          # Blazor/web UI (Redis output cache)
│   └── Services/
│       ├── Academic/                 # Courses, curriculum, departments, programmes
│       ├── Attendance/               # Session attendance, condonation
│       ├── Examination/              # Schedules, hall tickets, marks, results
│       ├── Faculty/                  # Faculty profiles, assignments, publications
│       ├── Fee/                      # Fee structures, payments, scholarships
│       ├── Hostel/                   # Rooms, allotments, complaints
│       ├── Identity/                 # Auth, tenants, users (OpenIddict + ASP.NET Identity)
│       ├── Notification/             # Email/SMS dispatch (MailKit + Kafka consumer)
│       └── Student/                  # Student profiles, enrollment
├── src/Tests/
│   ├── AppHost.IntegrationTests/     # Full-stack integration (Testcontainers)
│   ├── Identity.IntegrationTests/    # Identity API integration
│   ├── Kafka.IntegrationTests/       # Outbox + broker integration
│   └── TenantIsolation.Tests/        # Multi-tenancy row-level isolation
├── k8s/
│   ├── base/                         # Kustomize base (all services, infra, RBAC)
│   └── overlays/dev-local/           # Local overrides (image pull policy, env patches)
├── .github/workflows/                # CI: build, test, governance, security, deploy
└── src/Governance/
    ├── Aegis.Core/                   # Roslyn-based Clean Architecture linter
    └── Aegis.Tests/                  # Linter tests
```

---

## 3. SERVICE ANATOMY — EVERY SERVICE IS IDENTICAL

Each service follows **strict 4-layer Clean Architecture**. No exceptions.

```
Services/{Name}/
├── {Name}.Domain/          # Layer 1 — pure business logic, zero external dependencies
├── {Name}.Application/     # Layer 2 — CQRS handlers, interfaces, validators
├── {Name}.Infrastructure/  # Layer 3 — EF Core, Kafka, repositories
├── {Name}.API/             # Layer 4 — HTTP endpoints, DI wiring, middleware
└── {Name}.Tests/           # xUnit tests for Domain + Application
```

### Dependency Rule (enforced by Aegis linter)

```
Domain        <-- no dependencies on other layers
Application   <-- depends on Domain only
Infrastructure <-- depends on Application (and Domain transitively)
API           <-- depends on Application + Infrastructure (wiring only)
Tests         <-- depends on Domain + Application (never Infrastructure directly)
```

**Violations the linter catches:**
- Infrastructure referenced from Domain or Application
- Kafka (`Confluent.Kafka`) in API layer — belongs in Infrastructure
- EF Core DbContext in Domain — belongs in Infrastructure
- `HttpClient` in Domain or Application

---

## 4. ARCHITECTURE PATTERNS

### 4.1 CQRS with MediatR

Every feature is a Command or Query. No service calls between layers except through MediatR.

```csharp
// Command — mutates state
public sealed record CreateCourseCommand(
    Guid TenantId,
    string Code,
    string Title,
    int Credits) : IRequest<Guid>;

// Command Handler — in Application layer
internal sealed class CreateCourseCommandHandler
    : IRequestHandler<CreateCourseCommand, Guid>
{
    private readonly ICourseRepository _courses;
    private readonly IUnitOfWork _uow;

    public CreateCourseCommandHandler(ICourseRepository courses, IUnitOfWork uow)
    {
        _courses = courses;
        _uow = uow;
    }

    public async Task<Guid> Handle(CreateCourseCommand cmd, CancellationToken ct)
    {
        var course = Course.Create(cmd.TenantId, cmd.Code, cmd.Title, cmd.Credits);
        await _courses.AddAsync(course, ct);
        await _uow.SaveChangesAsync(ct);
        return course.Id;
    }
}

// Query — reads state, never mutates
public sealed record GetCourseQuery(Guid TenantId, Guid CourseId)
    : IRequest<CourseDto?>;
```

**Naming conventions:**
| Type | Pattern | Example |
|---|---|---|
| Command | `{Verb}{Entity}Command` | `CreateCourseCommand` |
| Query | `Get{Entity}Query` / `List{Entity}sQuery` | `GetCourseQuery` |
| Handler | `{Command/Query}Handler` | `CreateCourseCommandHandler` |
| Validator | `{Command}Validator` | `CreateCourseCommandValidator` |
| Result/DTO | `{Entity}Dto` / `{Entity}Response` | `CourseDto` |

### 4.2 Domain Model Pattern

```csharp
// Domain entity — in {Name}.Domain/Entities/
public sealed class Course : AggregateRoot  // from UMS.SharedKernel
{
    public Guid TenantId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public int Credits { get; private set; }
    public bool IsActive { get; private set; }

    private Course() { } // EF Core constructor

    public static Course Create(Guid tenantId, string code, string title, int credits)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        if (credits <= 0) throw new DomainException("Credits must be positive.");

        var course = new Course
        {
            Id       = Guid.NewGuid(),
            TenantId = tenantId,
            Code     = code.ToUpperInvariant(),
            Title    = title,
            Credits  = credits,
            IsActive = true
        };

        course.RaiseDomainEvent(new CourseCreatedEvent(course.Id, tenantId, code));
        return course;
    }

    public void Deactivate()
    {
        if (!IsActive) return; // idempotent
        IsActive = false;
        RaiseDomainEvent(new CourseDeactivatedEvent(Id, TenantId));
    }
}
```

### 4.3 Transactional Outbox Pattern

**Never publish directly to Kafka from a handler.** Always write to the outbox table in the same transaction, then let the `OutboxRelayService` publish asynchronously.

```csharp
// Domain event raised inside aggregate
public sealed record CourseCreatedEvent(Guid CourseId, Guid TenantId, string Code)
    : IDomainEvent;

// Infrastructure: DomainEventDispatcherInterceptor converts domain events
// to OutboxMessage rows during SaveChangesAsync — automatic, no handler change needed.

// OutboxMessage schema (in each service's DbContext)
public sealed class OutboxMessage
{
    public Guid     Id          { get; init; } = Guid.NewGuid();
    public string   Type        { get; init; } = string.Empty;  // event type name
    public string   Payload     { get; init; } = string.Empty;  // JSON
    public Guid     TenantId    { get; init; }
    public DateTime OccurredAt  { get; init; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public string?  Error        { get; set; }
}
```

### 4.4 Multi-Tenancy

Every DB query is automatically scoped to `TenantId`. Never query without it.

```csharp
// Infrastructure: all DbContext queries use global query filters
protected override void OnModelCreating(ModelBuilder builder)
{
    // Applied automatically to every entity implementing ITenantScoped
    builder.Entity<Course>().HasQueryFilter(c => c.TenantId == _tenantContext.TenantId);
}

// API: TenantMiddleware resolves X-Tenant-Id header → ITenantContext
// Registration in Program.cs — must be before UseAuthorization()
app.UseTenantResolution();
```

### 4.5 FluentValidation

Every command has a validator. Registered automatically via DI assembly scan.

```csharp
public sealed class CreateCourseCommandValidator
    : AbstractValidator<CreateCourseCommand>
{
    public CreateCourseCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(10)
            .Matches(@"^[A-Z0-9]+$").WithMessage("Code must be alphanumeric uppercase.");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Credits).InclusiveBetween(1, 10);
    }
}
```

---

## 5. SHAREDKERNEL — WHAT BELONGS, WHAT DOES NOT

`src/Shared/UMS.SharedKernel/` is referenced by every service. Keep it lean.

**ALLOWED in SharedKernel:**
```csharp
// Base classes
public abstract class Entity { public Guid Id { get; protected set; } }
public abstract class AggregateRoot : Entity, IAggregateRoot
{
    private readonly List<IDomainEvent> _events = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _events.AsReadOnly();
    public void ClearDomainEvents() => _events.Clear();
    protected void RaiseDomainEvent(IDomainEvent e) => _events.Add(e);
}
public abstract class ValueObject { /* equality by value */ }

// Interfaces
public interface IDomainEvent : INotification { }
public interface IAggregateRoot { IReadOnlyList<IDomainEvent> DomainEvents { get; } }
public interface IRepository<T> where T : AggregateRoot { }
public interface IUnitOfWork { Task<int> SaveChangesAsync(CancellationToken ct); }
public interface ICurrentTenant { Guid TenantId { get; } string Slug { get; } }

// Common exceptions
public class DomainException : Exception { }
public class NotFoundException : Exception { }
public class ValidationException : Exception { }

// MediatR pipeline behaviors
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> { }
```

**NEVER in SharedKernel:**
- Service-specific entities or logic
- `HttpClient` or HTTP concerns
- Direct Kafka producer/consumer
- EF Core `DbContext` subclasses
- Any `using` of a specific service's namespace

---

## 6. DEPENDENCY INJECTION PATTERN — EVERY SERVICE

### Application layer DI

```csharp
// {Name}.Application/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });
        return services;
    }
}
```

### Infrastructure layer DI

```csharp
// {Name}.Infrastructure/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AcademicDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("AcademicDb")));

        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AcademicDbContext>());

        // Outbox relay
        services.AddHostedService<OutboxRelayService>();

        return services;
    }
}
```

### API layer DI (Program.cs pattern)

```csharp
// {Name}.API/Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();           // OTel, Serilog, health, resilience
builder.Services.AddApplication();      // MediatR, validators
builder.Services.AddInfrastructure(builder.Configuration); // EF, repos, outbox

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* JWT config */ });

builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseServiceDefaults();
app.UseTenantResolution();          // MUST be before UseAuthorization
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();          // /health, /alive from ServiceDefaults
// Map feature endpoints here

app.Run();
```

---

## 7. REPOSITORY PATTERN

```csharp
// Interface — in Application layer
public interface ICourseRepository
{
    Task<Course?> FindByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default);
    Task<List<Course>> ListByDepartmentAsync(Guid tenantId, Guid deptId, CancellationToken ct = default);
    Task AddAsync(Course course, CancellationToken ct = default);
    Task UpdateAsync(Course course, CancellationToken ct = default);
}

// Implementation — in Infrastructure layer
internal sealed class CourseRepository : ICourseRepository
{
    private readonly AcademicDbContext _db;

    public CourseRepository(AcademicDbContext db) => _db = db;

    public Task<Course?> FindByIdAsync(Guid tenantId, Guid id, CancellationToken ct)
        => _db.Courses
              .Where(c => c.TenantId == tenantId && c.Id == id)
              .FirstOrDefaultAsync(ct);

    public async Task AddAsync(Course course, CancellationToken ct)
        => await _db.Courses.AddAsync(course, ct);

    public Task UpdateAsync(Course course, CancellationToken ct)
    {
        _db.Courses.Update(course);
        return Task.CompletedTask;
    }
}
```

---

## 8. EF CORE CONVENTIONS

```csharp
// DbContext — in Infrastructure/Persistence/
public sealed class AcademicDbContext : DbContext, IUnitOfWork
{
    private readonly ICurrentTenant _tenant;

    public AcademicDbContext(
        DbContextOptions<AcademicDbContext> options,
        ICurrentTenant tenant) : base(options)
    {
        _tenant = tenant;
    }

    public DbSet<Course>     Courses     => Set<Course>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("academic");
        builder.ApplyConfigurationsFromAssembly(typeof(AcademicDbContext).Assembly);

        // Global tenant filter on every entity
        builder.Entity<Course>().HasQueryFilter(c => c.TenantId == _tenant.TenantId);
    }
}

// Entity configuration — in Infrastructure/Persistence/Configurations/
internal sealed class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Code).HasMaxLength(10).IsRequired();
        builder.Property(c => c.Title).HasMaxLength(200).IsRequired();
        builder.HasIndex(c => new { c.TenantId, c.Code }).IsUnique();
        builder.Property(c => c.RowVersion).IsRowVersion(); // optimistic concurrency
    }
}
```

**Migration commands:**
```powershell
$Svc = "Academic"
$Repo = "C:\Users\harsh\source\repos\AspireApp1"
Set-Location "$Repo\src\Services\$Svc\$Svc.Infrastructure"
dotnet ef migrations add <MigrationName> --startup-project "..\$Svc.API"
dotnet ef database update --startup-project "..\$Svc.API"
```

---

## 9. INFRASTRUCTURE — EXTERNAL SYSTEMS

| System | Package | Connection string key | Used in |
|---|---|---|---|
| PostgreSQL | `Npgsql.EntityFrameworkCore.PostgreSQL` | `{Service}Db` | All services |
| Kafka | `Confluent.Kafka` | `kafka` (from ConfigMap) | Infrastructure only |
| Seq | Serilog sink | `Seq__ServerUrl` | ServiceDefaults |
| Jaeger | OTel exporter | `OTEL_EXPORTER_OTLP_ENDPOINT` | ServiceDefaults |
| Redis | `Aspire.StackExchange.Redis.OutputCaching` | (Aspire wired) | Web UI |
| OpenIddict | `OpenIddict.AspNetCore` + EFCore | `IdentityDb` | Identity service only |

### Kafka producer pattern (Infrastructure only)

```csharp
// Always in Infrastructure — never in API or Application
internal sealed class CourseEventPublisher
{
    private readonly IProducer<string, string> _producer;

    public async Task PublishAsync(string topic, string key, string payload)
    {
        await _producer.ProduceAsync(topic, new Message<string, string>
        {
            Key   = key,
            Value = payload
        });
    }
}
```

---

## 10. KUBERNETES — KEY FACTS

```yaml
# Namespace: ums
# Every service has: Deployment + Service + (optionally) HPA

# Secret: ums-secrets  (NEVER commit real values — use placeholders only)
# ConfigMap: ums-config (non-sensitive config, patched by overlay)

# Apply secrets from local env file:
kubectl create secret generic ums-secrets \
  --from-env-file=ums-secrets.env.local -n ums \
  --dry-run=client -o yaml | kubectl apply -f -

# Common commands:
kubectl get pods -n ums
kubectl logs <pod> -n ums --follow
kubectl describe pod <pod> -n ums
kubectl apply -k k8s/overlays/dev-local    # dev deployment
kubectl rollout restart deployment/<name> -n ums
```

**K8s environment variables every service reads:**

| Key | Source | Purpose |
|---|---|---|
| `ConnectionStrings__{Service}Db` | Secret | PostgreSQL connection |
| `ConnectionStrings__kafka` | ConfigMap | Kafka broker |
| `Auth__Authority` | ConfigMap | Identity API URL |
| `Seq__ServerUrl` | ConfigMap | Structured log sink |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | ConfigMap | Jaeger trace export |
| `REGION_WRITE_ALLOWED` | ConfigMap overlay | Primary/replica routing |
| `ASPNETCORE_ENVIRONMENT` | ConfigMap | `Production` in cluster |

---

## 11. TESTING STRATEGY

```
{Name}.Tests/
├── Domain/         # Pure unit tests — no mocks, no DB, just domain logic
├── Application/    # Handler tests — mock repositories via Moq/NSubstitute
└── Fakers/         # Bogus-based test data builders
```

### Domain test pattern (pure, fast)

```csharp
public sealed class CourseTests
{
    [Fact]
    public void Create_WithValidData_RaisesCourseCreatedEvent()
    {
        var course = Course.Create(Guid.NewGuid(), "CS101", "Intro to CS", 3);

        course.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CourseCreatedEvent>();
    }

    [Fact]
    public void Create_WithEmptyCode_ThrowsDomainException()
    {
        var act = () => Course.Create(Guid.NewGuid(), "", "Title", 3);
        act.Should().Throw<ArgumentException>();
    }
}
```

### Application handler test pattern

```csharp
public sealed class CreateCourseCommandHandlerTests
{
    private readonly Mock<ICourseRepository> _courses = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    [Fact]
    public async Task Handle_ValidCommand_ReturnsNewId()
    {
        var handler = new CreateCourseCommandHandler(_courses.Object, _uow.Object);
        var cmd = new CreateCourseCommand(Guid.NewGuid(), "CS101", "Intro to CS", 3);

        var id = await handler.Handle(cmd, CancellationToken.None);

        id.Should().NotBeEmpty();
        _courses.Verify(r => r.AddAsync(It.IsAny<Course>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

**Run tests:**
```powershell
dotnet test C:\Users\harsh\source\repos\AspireApp1\src\Services\Academic\Academic.Tests --logger "console;verbosity=minimal"
dotnet test C:\Users\harsh\source\repos\AspireApp1\src\Tests\TenantIsolation.Tests  # Testcontainers — needs Docker
```

---

## 12. CODEGEN RULES — HOW AI GENERATES FILES

### File naming convention

```
[project]__[service]__[feature]__[YYYYMMDD_HHMM].[ext]
```

Examples:
```
ums__academic__create-course-command__20260313_1000.cs
ums__academic__course-repository__20260313_1005.cs
ums__student__enroll-command-handler__20260313_1010.cs
```

### AI response format — every code generation

```
FILE:
ums__academic__create-course-command__20260313_1000.cs

PATH:
src/Services/Academic/Academic.Application/Features/Courses/Commands/

COMMAND:
Copy-Item "C:\Users\harsh\Downloads\ums-drop\ums__academic__create-course-command__20260313_1000.cs" `
  "C:\Users\harsh\source\repos\AspireApp1\src\Services\Academic\Academic.Application\Features\Courses\Commands\CreateCourseCommand.cs" -Force

VERIFY:
dotnet build C:\Users\harsh\source\repos\AspireApp1\src\Services\Academic\Academic.API --no-incremental
```

### Integration workflow (every time)

```powershell
# 1. Download generated file to drop folder
$Drop = "C:\Users\harsh\Downloads\ums-drop"
$Repo = "C:\Users\harsh\source\repos\AspireApp1"

# 2. Copy to destination (AI provides exact command above)
Copy-Item "$Drop\<filename>" "$Repo\<path>\<TargetName>.cs" -Force

# 3. Build the affected service
dotnet build "$Repo\src\Services\<Service>\<Service>.API" --no-incremental

# 4. Run tests
dotnet test "$Repo\src\Services\<Service>\<Service>.Tests"

# 5. Commit
git add src\Services\<Service>\
git commit -m "feat(<service>): <description>"
```

---

## 13. PACKAGE RULES — CRITICAL

**Central Package Management is ACTIVE.** `Directory.Packages.props` owns all versions.

- **NEVER** add `Version="..."` to any `PackageReference` in `.csproj` files
- **ALWAYS** add new packages to `Directory.Packages.props` first, then reference in `.csproj`
- **ASP.NET Core types** (`IdentityUser`, `IHttpContextAccessor`, etc.) → use `<FrameworkReference Include="Microsoft.AspNetCore.App" />`, NOT a PackageReference
- **EF Core in Domain/Application** → NEVER. Use `FrameworkReference` if ASP.NET Core types needed; keep EF Core in Infrastructure only

### Package layer rules

| Package | Domain | Application | Infrastructure | API |
|---|---|---|---|---|
| `MediatR` | - | YES | YES | YES |
| `FluentValidation` | - | YES | - | - |
| `Microsoft.EntityFrameworkCore` | - | - | YES | (Design only) |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | - | - | YES | - |
| `Confluent.Kafka` | - | - | YES | - |
| `Microsoft.AspNetCore.App` (FrameworkRef) | if needed | if needed | YES | YES |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | - | - | YES (Identity only) | - |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | - | - | - | YES |

---

## 14. SECURITY RULES (NON-NEGOTIABLE)

1. **`k8s/base/secret.yaml` contains ONLY empty placeholders** — real values live in `ums-secrets.env.local` (gitignored)
2. **`ums-secrets.env.local` is gitignored** — never commit it
3. **`_backups/` is gitignored** — never commit build artifacts or backup files
4. **`binDebug/` is gitignored** — never commit IDE build outputs
5. **Connection strings** — never hardcode; always read from `IConfiguration`
6. **OpenIddict keys** — regenerate RSA key pairs before any production deployment
7. **`appsettings.Production.json`** — must contain NO secrets; use environment variables or K8s secrets

---

## 15. GOVERNANCE — AEGIS LINTER

The `Aegis.Core` + `Ums.Cli` tooling enforces Clean Architecture automatically.

```powershell
# Run architecture scan (from repo root)
.\dev.ps1 scan

# Or directly
dotnet run --project src\Cli\Ums.Cli -- scan --solution AspireApp1.sln

# Baseline snapshot (after intentional architecture change)
dotnet run --project src\Cli\Ums.Cli -- snapshot
```

Violations are stored as `.snap.json` files in `src/.ums/snapshots/`. CI pipeline (`governance.yml`) fails the build on any new violation.

---

## 16. CI/CD PIPELINES

| Workflow | Trigger | Does |
|---|---|---|
| `ci.yml` | PR + push to main | build, test, Aegis scan |
| `governance.yml` | PR | architecture violation check |
| `security.yml` | Push | secret scanning, dependency audit |
| `docker-build.yml` | Push to main | build + push images |
| `deploy.yml` | Manual / tag | kubectl apply to cluster |

---

## 17. INSTANT CONTEXT COMMANDS

Use these to generate a ZIP of any service for AI upload:

```powershell
# Single service (source files only, no build artifacts)
$Svc = "Academic"   # Change this
$Repo = "C:\Users\harsh\source\repos\AspireApp1"
$Out  = "C:\Users\harsh\Downloads\ums-$Svc-$(Get-Date -Format 'yyyyMMdd_HHmm').zip"
$Tmp  = Join-Path $env:TEMP "ums-ctx-$Svc"
Remove-Item $Tmp -Recurse -Force -ErrorAction SilentlyContinue
Copy-Item "$Repo\src\Services\$Svc" $Tmp -Recurse -Force
Get-ChildItem $Tmp -Recurse -Directory |
  Where-Object { $_.Name -in @("bin","obj",".vs","binDebug","binRelease") } |
  Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
Compress-Archive "$Tmp\*" $Out -Force
Remove-Item $Tmp -Recurse -Force
Write-Host "Ready: $Out" -ForegroundColor Green

# Skeleton only (csproj + json + yaml — no .cs files, tiny size)
$Out = "C:\Users\harsh\Downloads\ums-skeleton-$(Get-Date -Format 'yyyyMMdd_HHmm').zip"
$Tmp = Join-Path $env:TEMP "ums-skeleton"
Remove-Item $Tmp -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory $Tmp | Out-Null
Get-ChildItem $Repo -Recurse -Include "*.csproj","*.sln","*.json","*.yaml","*.yml" |
  Where-Object { $_.FullName -notmatch "\\(bin|obj|\.vs|binDebug)\\" } |
  ForEach-Object {
    $Dest = Join-Path $Tmp $_.FullName.Substring($Repo.Length)
    New-Item -ItemType Directory -Force (Split-Path $Dest) | Out-Null
    Copy-Item $_.FullName $Dest
  }
Compress-Archive "$Tmp\*" $Out -Force
Remove-Item $Tmp -Recurse -Force
Write-Host "Ready: $Out" -ForegroundColor Green
```

---

## 18. CURRENT KNOWN ISSUES (BACKLOG)

| # | Severity | Issue | Status |
|---|---|---|---|
| 1 | DONE | `k8s/base/secret.yaml` had real credentials committed | Fixed — Step 1 |
| 2 | DONE | `Identity.Domain` + `Identity.Application` referenced `EF Identity` package | Fixed — Step 2 |
| 3 | TODO | `Faculty.API` + `Student.API` have `Confluent.Kafka` directly in API layer | Step 3 |
| 4 | TODO | `ApiGateway` missing `ServiceDefaults` (no OTel, no health checks) | Step 4 |
| 5 | TODO | `ApiService` (Aspire template stub) still registered in `AppHost` — dead code | Step 5 |

---

## 19. QUICK DIAGNOSTIC CHECKLIST

Before every coding session, verify:

```powershell
$Repo = "C:\Users\harsh\source\repos\AspireApp1"

# 1. Build entire solution
dotnet build $Repo --no-incremental

# 2. Check for accidental secrets in git index
git -C $Repo diff --cached --name-only | Select-String "secret|password|key|token"

# 3. Check for binDebug in git index
git -C $Repo ls-files | Select-String "binDebug|binRelease"

# 4. Run architecture linter
dotnet run --project "$Repo\src\Cli\Ums.Cli" -- scan
```

---

*Last updated: 2026-03-13 | Maintained by Harsh | UMS Platform Engineering*
