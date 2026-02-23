# UMS Platform — Architecture Bible
> ⚠️ IMMUTABLE DOCUMENT — Changes require explicit approval and must be logged with reason.
> Last updated: 2026-02-23 | Session 14
> Change log: Session 13 patterns applied (Session 14 opener) — Phase 8 migration, Phase 9 observability, ADR-006 enforcement, AppHost test fixes, Phase 10 rate limiting + versioning

---

## 🔒 What This Document Is

This document contains **facts that never change** without a deliberate architectural decision.
If Claude or a developer suggests changing anything here mid-session, **stop and flag it explicitly**.
No bypassing without stating reason + getting confirmation.

---

## Tech Stack (Locked Versions)

| Technology | Version | Lock Reason |
|---|---|---|
| .NET / C# | 10.0 / 13 | Aspire 9.3 compatibility |
| .NET Aspire | 9.3.1 | DO NOT upgrade to 13.x — breaking API changes |
| EF Core | 10.0.3 | Npgsql compatibility |
| Npgsql.EFCore | 10.0.0 | Must match EF Core major |
| MediatR | 12.4.1 | DO NOT upgrade to 14 — breaking handler interface changes |
| FluentValidation | 11.11.0 | DO NOT upgrade to 12 — breaking validator changes |
| FluentAssertions | 7.0.0 | DO NOT upgrade to 8 — breaking assertion syntax |
| OpenIddict | 5.8.0 | DO NOT upgrade to 7 — breaking token store changes |
| OpenIddict.Server.AspNetCore | 5.8.0 | Must match OpenIddict version |
| Confluent.Kafka | 2.13.1 | Pinned — Academic.Infrastructure incompatible with newer |
| Serilog.AspNetCore | 8.0.3 | DO NOT upgrade to 10 — breaking sink changes |
| Serilog.Sinks.Seq | 8.0.0 | Must match Serilog.AspNetCore 8.0.3 — added Session 13 to ServiceDefaults |
| OpenTelemetry | 1.15.0 | Aspire instrumentation compatibility |
| Aspire.Hosting.Seq | 9.3.1 | Must match .NET Aspire version |
| Testcontainers | 3.10.0 | DO NOT upgrade to 4 — breaking container API |
| Testcontainers.PostgreSql | 3.10.0 | Must match Testcontainers version |
| Microsoft.AspNetCore.Mvc.Testing | 10.0.3 | Must match .NET version |
| YARP | 2.3.0 | Gateway — stable release |
| Asp.Versioning.Http | latest stable | Gateway only — API versioning, added Phase 10 |
| xunit.runner.visualstudio | 3.1.5 | |
| Microsoft.NET.Test.Sdk | 18.0.1 | |

---

## Solution Structure (Immutable Paths)

```
C:\Users\harsh\source\repos\AspireApp1\
├── AspireApp1.slnx                    ← ⚠️ .slnx NOT .sln — ALWAYS use for dotnet sln commands
├── ums-aliases.ps1                    ← PowerShell aliases
├── .github\workflows\ci.yml           ← GitHub Actions CI/CD
└── src\
    ├── AppHost\
    │   ├── AspireApp1.AppHost.csproj  ← ⚠️ EXACT csproj name — NOT AppHost.csproj
    │   └── AppHost.cs                 ← ⚠️ AppHost.cs NOT Program.cs
    ├── ServiceDefaults\
    ├── Shared\UMS.SharedKernel\
    ├── ApiGateway\                    ← YARP gateway (Phase 7)
    ├── BFF\                           ← Mobile aggregator (Phase 7B)
    ├── Services\
    │   ├── Identity\
    │   ├── Academic\
    │   ├── Student\
    │   ├── Attendance\
    │   ├── Examination\
    │   ├── Fee\
    │   ├── Notification\
    │   ├── Faculty\
    │   └── Hostel\
    └── Tests\
        ├── AppHost.IntegrationTests\
        ├── TenantIsolation.Tests\
        ├── Kafka.IntegrationTests\
        └── Identity.IntegrationTests\
```

---

## Service Registry

| Service | Aspire Name | DB Connection String | Kafka Topic | Port Pattern |
|---|---|---|---|---|
| Identity | `identity-api` | `IdentityDb` | `identity-events` | — |
| Academic | `academic-api` | `AcademicDb` | `academic-events` | — |
| Student | `student-api` | `StudentDb` | `student-events` | — |
| Attendance | `attendance-api` | `AttendanceDb` | `attendance-events` | — |
| Examination | `examination-api` | `ExaminationDb` | `examination-events` | — |
| Fee | `fee-api` | `FeeDb` | `fee-events` | — |
| Notification | `notification-api` | `NotificationDb` | `notification-events` | — |
| Faculty | `faculty-api` | `FacultyDb` | `faculty-events` | — |
| Hostel | `hostel-api` | `HostelDb` | `hostel-events` | — |
| API Gateway | `api-gateway` | — | — | — |
| BFF | `bff` | — | — | — |

### Observability Resources in AppHost (Phase 9)
```csharp
var seq    = builder.AddSeq("seq").ExcludeFromManifest();        // Aspire.Hosting.Seq 9.3.1
var jaeger = builder.AddContainer("jaeger", "jaegertracing/all-in-one", "1.57")
                    .WithEndpoint(port: 16686, targetPort: 16686, name: "ui")
                    .WithEndpoint(port: 4317,  targetPort: 4317,  name: "otlp-grpc")
                    .ExcludeFromManifest();
var jaegerOtlp = jaeger.GetEndpoint("otlp-grpc");
var seqUrl     = seq.GetEndpoint("http");

// Per service (all 11 projects — 9 services + gateway + BFF):
.WithReference(seq)
.WithEnvironment("Seq__ServerUrl", seqUrl)
.WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", jaegerOtlp)
```

### Observability Access URLs (when Aspire running)
- Seq UI: `http://localhost:5341`
- Jaeger UI: `http://localhost:16686`

---

## Architecture Decisions (ADRs — Never Reverse Without Discussion)

### ADR-001: Clean Architecture + DDD + CQRS
Every service follows: Domain → Application → Infrastructure → API
MediatR handles all commands/queries. No direct service-to-service HTTP calls.

### ADR-002: Multi-Tenant via TenantId on Every Entity
Every entity has `TenantId`. Every repo filters by `tenantId`. Proven by TenantIsolation.Tests (33 tests).

### ADR-003: Soft Delete
`HasQueryFilter(x => !x.IsDeleted)` on ALL entities. Never hard delete.

### ADR-004: Outbox Pattern for Kafka
All Kafka events go via OutboxMessage → OutboxRelayService. Never publish Kafka directly from domain/application layer.

### ADR-005: OpenIddict Passthrough Pattern
`/connect/token` is mapped as an inline minimal API endpoint in Identity.API Program.cs.
Event handler pattern (`PasswordGrantHandler`) is NOT used — do not revert.

### ADR-006: Gateway Owns Auth
As of Session 12: API Gateway validates JWTs. Individual services do NOT validate tokens.
Services trust the gateway and read tenant/user context from forwarded headers.

**Enforcement rules (added Session 13):**
- Individual services MUST NOT have `.RequireAuthorization()` on any endpoint
- Services have no `AddAuthentication()` / `UseAuthorization()` in their pipelines
- Applying `.RequireAuthorization()` without `UseAuthorization()` causes hard **runtime** exception — not a compile error
- TenantMiddleware reads `X-Tenant-Id` and `X-Tenant-Slug` from request headers — NOT from JWT claims

### ADR-007: Notification Uses Service Pattern (NOT MediatR)
Notification service uses direct service injection, not MediatR commands/queries.

### ADR-008: Aspire Service Discovery
No hardcoded URLs anywhere. All inter-service communication uses Aspire service discovery names.

### ADR-009: Rate Limiting — Tenant-Aware (Phase 10)
Gateway rate limiting partitions by `X-Tenant-Id` header (100 req/min per tenant).
Fallback to IP when no tenant header present (e.g. unauthenticated requests).
`/connect/token` uses strict named policy: 10 req/min per IP.
`Retry-After: 60` header always returned on 429 responses.

### ADR-010: API Versioning — Gateway Only (Phase 10)
Versioning lives exclusively at the gateway. Internal services are version-agnostic.
YARP `PathRemovePrefix` transform strips `/api/v1` before forwarding to downstream service.
External route pattern: `/api/v{version}/{service}/{**catch-all}`
Internal route (unchanged): `/api/{service}/{**catch-all}`
Package `Asp.Versioning.Http` installed on ApiGateway only — NOT on individual services.

---

## Immutable Code Patterns

### Kafka Connection String
```csharp
configuration.GetConnectionString("kafka") ?? "localhost:9092"
```
Always this pattern. Never hardcode Kafka URL.

### IConfigurationManager Cast
```csharp
((IConfiguration)builder.Configuration).GetConnectionString("...")
```
Must cast to `IConfiguration` first — `IConfigurationManager` does not expose `GetConnectionString`.

### Health Check Method (Capital S)
```csharp
builder.AddNpgSqlHealthCheck("XxxDb")  // ← capital S in NpgSql
```
Package: `AspNetCore.HealthChecks.NpgSql 8.0.2`

### OpenIddict Request Retrieval
```csharp
// ❌ NEVER use — does not work even with correct packages:
httpContext.GetOpenIddictServerRequest()

// ✅ ALWAYS use:
httpContext.Features.Get<OpenIddictServerAspNetCoreFeature>()?.Transaction?.Request
```

### OpenIddict Username Format
```
tenantSlug|email   e.g.  ums|superadmin@ums.com
```
Handler splits on `|` — first part is tenant slug, second is email.

### Seeded Credentials (Dev/Test Only)
| Field | Value |
|---|---|
| Email | `superadmin@ums.com` |
| Password | `Admin@1234` |
| Tenant Slug | `ums` |
| Client ID | `api-gateway` |
| Client Secret | `api-gateway-secret` |

### OutboxMessage.Create() — Exists Only In:
- Academic.Domain
- Identity.Domain
- Student.Domain

Fee, Examination, Hostel → use object initializer directly (no Create() factory).

### Faculty DbSet Name
```csharp
DbSet<Faculty> Faculty  // NOT Faculties
```

### Namespace Alias Pattern (Required for conflicting names)
```csharp
using FacultyEntity = Faculty.Domain.Entities.Faculty;
```
Apply proactively whenever entity name matches namespace.

### No RequireAuthorization on Service Endpoints (ADR-006)
```csharp
// ❌ NEVER — causes hard runtime exception without UseAuthorization() in pipeline:
app.MapGet("/api/students", handler).RequireAuthorization();

// ✅ ALWAYS — gateway owns auth, services have no auth middleware:
app.MapGet("/api/students", handler);
```

### GetTenantId — Always Read from Header (ADR-006)
```csharp
// ❌ NEVER — no JWT in individual services:
httpContext.User.FindFirstValue("tenant_id")

// ✅ ALWAYS — gateway forwards X-Tenant-Id header:
httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault()
```

### MigrateWithRetryAsync — Required Pattern (Phase 8)
Never use bare `db.Database.Migrate()` or `db.Database.MigrateAsync()` at startup.
Always use the local static function below. Applied to: Examination, Fee, Student, Identity.

```csharp
// Call site — replaces bare using (var scope...) block:
await MigrateWithRetryAsync<XxxDbContext>(app.Services);

// Local static function at bottom of Program.cs:
static async Task MigrateWithRetryAsync<TDb>(IServiceProvider services,
    int maxRetries = 5, int delaySeconds = 3) where TDb : DbContext
{
    using var scope = services.CreateScope();
    var db     = scope.ServiceProvider.GetRequiredService<TDb>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<TDb>>();
    for (int i = 1; i <= maxRetries; i++)
    {
        try
        {
            await db.Database.MigrateAsync();
            logger.LogInformation("[Migration] {Db} succeeded on attempt {Attempt}", typeof(TDb).Name, i);
            return;
        }
        catch (Exception ex) when (i < maxRetries)
        {
            logger.LogWarning("[Migration] {Db} attempt {Attempt}/{Max} failed: {Msg}. Retrying in {Delay}s...",
                typeof(TDb).Name, i, maxRetries, ex.Message, delaySeconds);
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
        }
    }
    await db.Database.MigrateAsync(); // final attempt — throws naturally
}
```

⚠️ `MigrationExtensions` CANNOT live in SharedKernel — SharedKernel has no EF Core reference.
Always use local static function inside each service's `Program.cs`.

### Seq Configuration Key
```csharp
// Env var injected by Aspire (double underscore = nested config binding):
"Seq__ServerUrl"

// Read in SerilogExtensions.cs:
builder.Configuration["Seq:ServerUrl"]
```

### Jaeger — ContainerResource Wiring (Phase 9)
`jaeger` is `AddContainer()` → returns `ContainerResource` → does NOT implement `IResourceWithConnectionString`.
```csharp
// ❌ NEVER — CS1503 compile error:
.WithReference(jaeger)

// ✅ ALWAYS — wire via environment variable only:
.WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", jaeger.GetEndpoint("otlp-grpc"))
```
`AddSeq()` IS a proper Aspire resource → supports `.WithReference(seq)` ✅

### TenantMiddleware — Header Based (ADR-006, updated Session 13)
`ServiceDefaults\TenantMiddleware.cs` reads `X-Tenant-Id` and `X-Tenant-Slug` from request headers.
Does NOT read JWT claims. `using System.Security.Claims` removed.

### Rate Limiter — Named Policy Pattern (Phase 10)
```csharp
// ❌ NEVER — AddFixedWindowLimiter is an extension method, requires explicit using:
// (missing using causes CS1061)

// ✅ ALWAYS add at top of Program.cs:
using Microsoft.AspNetCore.RateLimiting;

// Named policy for token endpoint:
options.AddFixedWindowLimiter("token-endpoint", o =>
{
    o.PermitLimit = 10;
    o.Window      = TimeSpan.FromMinutes(1);
    o.QueueLimit  = 0;
});

// Named policy for all other routes — tenant-partitioned:
options.AddPolicy("tenant-fixed", httpContext =>
{
    var tenantId     = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
    var partitionKey = !string.IsNullOrEmpty(tenantId)
        ? $"tenant:{tenantId}"
        : $"ip:{httpContext.Connection.RemoteIpAddress}";
    return RateLimitPartition.GetFixedWindowLimiter(partitionKey,
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 100, Window = TimeSpan.FromMinutes(1) });
});

options.RejectionStatusCode = 429;
options.OnRejected = async (context, ct) =>
{
    context.HttpContext.Response.Headers["Retry-After"] = "60";
    await context.HttpContext.Response.WriteAsync("Rate limit exceeded. Please retry after 60 seconds.", ct);
};
```

### API Versioning — YARP PathRemovePrefix (Phase 10)
```json
// appsettings.json — versioned route with transform:
"academic-route": {
  "ClusterId": "academic-cluster",
  "Match": { "Path": "/api/v{version:apiVersion}/academic/{**catch-all}" },
  "RateLimiterPolicy": "tenant-fixed",
  "Transforms": [{ "PathRemovePrefix": "/api/v1" }]
}
// Result: /api/v1/academic/courses → forwarded as /api/academic/courses
// Services receive unchanged internal paths — fully version-agnostic
```

---

## Test Architecture Facts

### Integration Test WebFactory Pattern
```csharp
services.RemoveAll<IHostedService>()  // kills Kafka relay in tests
```
Always strip hosted services in integration test factories.

### OpenIddict Test Fixture Rules
1. Set `Environment.SetEnvironmentVariable("ConnectionStrings__IdentityDb", connStr)` BEFORE factory builds
2. Re-register DbContext WITH `UseOpenIddict()` — otherwise stores fail silently → `invalid_grant`
3. IdentitySeeder: create roles BEFORE assigning (`roleMgr.CreateAsync` before `userMgr.AddToRoleAsync`)

### Fee/Exam Repositories
Must call `SaveChangesAsync` inside `AddAsync`/`UpdateAsync` — not deferred.

### StudentOutboxRelayService Location
Lives in `Student.API.Services` — NOT Student.Infrastructure (unlike all other services).

### AppHost Integration Tests — Three-Layer Fix (Session 13)
Root cause of 2/18 → 18/18 fix. Three independent layers all required:

**Layer 1** — Strip `.RequireAuthorization()` from ALL 18 endpoint files across all 9 services.
Causes hard runtime exception (not compile error) because services have no auth middleware per ADR-006.

**Layer 2** — Fix `GetTenantId` in endpoint files to read header not JWT claim:
```csharp
// ❌ Returns empty without JWT — wrong:
httpContext.User.FindFirstValue("tenant_id")

// ✅ Correct — reads gateway-forwarded header:
httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault()
```

**Layer 3** — Add default headers to `ServiceFixture.InitializeAsync()`:
```csharp
Client.DefaultRequestHeaders.Add("X-Tenant-Id", TestTenant.Id.ToString());
Client.DefaultRequestHeaders.Add("X-User-Id", Guid.NewGuid().ToString());
```
Without these headers, `GetTenantId` returns `Guid.Empty` → `Results.Unauthorized()`.

### ServiceDefaults Packages Added Session 13
- `Serilog.Sinks.Seq` version `8.0.0` — must match `Serilog.AspNetCore 8.0.3`

---

## Standard Checklist — New Service

When adding a new service, every item below is mandatory:

- [ ] Clean Architecture: Domain / Application / Infrastructure / API projects
- [ ] `OutboxMessage` in Domain.Common — use object initializer (no `Create()` unless Academic/Identity/Student)
- [ ] `OutboxRelayService` in Infrastructure.Kafka (or API.Services for Student pattern)
- [ ] `AddNpgSqlHealthCheck("XxxDb")` in Program.cs — capital S
- [ ] `AddSerilogDefaults()` + `UseSerilogDefaults()` in Program.cs
- [ ] `UseGlobalExceptionHandler()` in Program.cs — requires `using UMS.SharedKernel.Extensions`
- [ ] `HasQueryFilter(x => !x.IsDeleted)` on all entities
- [ ] All repos filter by `tenantId`
- [ ] **NO** `.RequireAuthorization()` on endpoints — gateway owns auth (ADR-006)
- [ ] Tenant read from `X-Tenant-Id` header — NOT from JWT claims
- [ ] `MigrateWithRetryAsync<TDb>` local static function in Program.cs — never bare `MigrateAsync()`
- [ ] Register in AppHost: `.WithReference(db).WithReference(kafka).WithReference(seq)`
- [ ] `.WithEnvironment("Seq__ServerUrl", seqUrl)`
- [ ] `.WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", jaegerOtlp)`
- [ ] Add `ub {service}` and `ut {service}` entries to `ums-aliases.ps1` hashtable (never separate functions)
- [ ] Add unit tests to `{Service}.Tests` project
- [ ] Add versioned YARP route to `appsettings.json` with `PathRemovePrefix` transform and `"tenant-fixed"` rate limiter policy
- [ ] Add `X-Tenant-Id` + `X-User-Id` default headers in integration test `ServiceFixture.InitializeAsync()`

---

## PowerShell Rules (Immutable)

### Alias File Location
```
C:\Users\harsh\source\repos\AspireApp1\ums-aliases.ps1
```
Loaded via: `. $PROFILE` (Microsoft.PowerShell_profile.ps1)

### Alias Naming Convention
- ✅ Use underscores: `ut_identity_int`
- ❌ Never use hyphens: `ut-identity-int` — registers as Alias type not Function, silently fails

### Profile Reload Rule
Never chain `. $PROFILE` and a command on the same line — must be two separate Enter-confirmed commands.

### Multiline Replace Rule
PowerShell `-replace` with multiline strings fails silently on whitespace/encoding mismatch.
Use line-index approach instead: `$lines[$idx] = "..."`.

### PowerShell Syntax
- Use `;` not `&&` for command chaining
- Use here-strings + `Set-Content` — never paste raw C# inline
- Never use `-Recurse` with `Select-String` directly on path — use `Get-ChildItem | Select-String`
- `String.Replace()` fails silently on whitespace/newline mismatch — use `Set-Content` full file rewrite
- `Select-String -Context` output truncates — use `Get-Content` for full file view

---

## Alias Reference (Complete)

| Alias | Action |
|---|---|
| `ub academic` | Build Academic API |
| `ub student` | Build Student API |
| `ub identity` | Build Identity API |
| `ub faculty` | Build Faculty API |
| `ub attendance` | Build Attendance API |
| `ub examination` | Build Examination API |
| `ub fee` | Build Fee API |
| `ub notification` | Build Notification API |
| `ub hostel` | Build Hostel API |
| `ub gateway` | Build API Gateway |
| `ub bff` | Build BFF |
| `ub defaults` | Build ServiceDefaults |
| `ub shared` | Build UMS.SharedKernel |
| `ub kafka` | Build Kafka.IntegrationTests |
| `ub apphost` | Build AppHost.IntegrationTests |
| `ub all` | Build entire solution |
| `ut academic` | Test Academic unit tests |
| `ut student` | Test Student unit tests |
| `ut identity` | Test Identity unit tests |
| `ut faculty` | Test Faculty unit tests |
| `ut attendance` | Test Attendance unit tests |
| `ut examination` | Test Examination unit tests |
| `ut fee` | Test Fee unit tests |
| `ut notification` | Test Notification unit tests |
| `ut hostel` | Test Hostel unit tests |
| `ut kafka` | Test Kafka integration tests |
| `ut isolation` | Test TenantIsolation tests |
| `ut apphost` | Test AppHost integration tests |
| `ut-all` | Run all 9 unit projects + total count |
| `ut_identity_int` | Test Identity.IntegrationTests |
| `2>&1 \| ue` | Pipe filter — strips Roslyn/LanguageServer noise |

---

## ⚠️ Critical: How ub and ut Are Implemented Internally

`ub` and `ut` are **switch-map functions** in `ums-aliases.ps1`. They take a service name parameter and look it up in a hashtable. They are NOT individual functions per service.

### ub internal structure (exact — do not deviate):
```powershell
function ub {
    param($svc)
    $map = @{
        "academic"     = "src/Services/Academic/Academic.API/Academic.API.csproj"
        "student"      = "src/Services/Student/Student.API/Student.API.csproj"
        "identity"     = "src/Services/Identity/Identity.API/Identity.API.csproj"
        "faculty"      = "src/Services/Faculty/Faculty.API/Faculty.API.csproj"
        "attendance"   = "src/Services/Attendance/Attendance.API/Attendance.API.csproj"
        "examination"  = "src/Services/Examination/Examination.API/Examination.API.csproj"
        "fee"          = "src/Services/Fee/Fee.API/Fee.API.csproj"
        "notification" = "src/Services/Notification/Notification.API/Notification.API.csproj"
        "hostel"       = "src/Services/Hostel/Hostel.API/Hostel.API.csproj"
        "gateway"      = "src/ApiGateway/ApiGateway.csproj"
        "bff"          = "src/BFF/BFF.csproj"
        "defaults"     = "src/ServiceDefaults/AspireApp1.ServiceDefaults.csproj"
        "shared"       = "src/Shared/UMS.SharedKernel/UMS.SharedKernel.csproj"
        "kafka"        = "src/Tests/Kafka.IntegrationTests/Kafka.IntegrationTests.csproj"
        "apphost"      = "src/Tests/AppHost.IntegrationTests/AppHost.IntegrationTests.csproj"
        "all"          = "AspireApp1.slnx"
    }
    if ($map[$svc]) { dotnet build $map[$svc] -v q 2>&1 | ue }
    else { Write-Host "Unknown: $svc. Options: $($map.Keys -join ', ')" }
}
```

### Rules for adding new services to ub/ut:
- ✅ ALWAYS add as a new key-value pair inside the `$map = @{ }` hashtable
- ❌ NEVER create a separate `function ub_newservice { }` — this breaks the pattern
- ❌ NEVER create `function ub-newservice { }` — hyphens fail in PowerShell
- The `"all"` key must always point to `AspireApp1.slnx` NOT `AspireApp1.sln`

### How to add a new service (correct pattern):
```powershell
# Find the closing } of the $map hashtable in ub and add before it:
"newservice" = "src/Services/NewService/NewService.API/NewService.API.csproj"
```

---

## Known Gotchas (Cumulative — Never Repeat These Mistakes)

| Session | Gotcha |
|---|---|
| 12 | `httpContext.GetOpenIddictServerRequest()` does not work — use `Features.Get<>()` pattern |
| 12 | AppHost.cs not Program.cs — Aspire entry point file naming |
| 13 | `.RequireAuthorization()` without `UseAuthorization()` causes hard **runtime** exception — not compile error |
| 13 | PowerShell `String.Replace()` fails silently on whitespace/newline mismatch — use `Set-Content` full rewrite |
| 13 | `jaeger` is `ContainerResource` — `.WithReference(jaeger)` causes CS1503. Use `.WithEnvironment()` only |
| 13 | `AddSeq()` returns proper Aspire resource → supports `.WithReference(seq)` ✅ |
| 13 | AppHost csproj is `AspireApp1.AppHost.csproj` — NOT `AppHost.csproj` |
| 13 | `MigrationExtensions` cannot live in SharedKernel — no EF Core reference there |
| 13 | `Select-String -Context` output truncates in PowerShell — use `Get-Content` for full file view |
| 13 | `Serilog.Sinks.Seq` version must be `8.0.0` to match `Serilog.AspNetCore 8.0.3` |
| 14 | `AddFixedWindowLimiter` is an extension method — requires `using Microsoft.AspNetCore.RateLimiting` |