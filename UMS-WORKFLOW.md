# UMS — Universal AI Development Context
> Auto-updated from actual source code — 2026-03-14
> .NET 10 | Aspire 9.3.1 | Kubernetes | Minikube

---

## 1. PROJECT IDENTITY

| Property | Value |
|---|---|
| Solution file | `UMS.slnx` (NOT AspireApp1.slnx — that is gone) |
| Repo (Windows) | `C:\Users\harsh\source\repos\AspireApp1` |
| Repo (WSL2) | `/mnt/c/Users/harsh/source/repos/AspireApp1` |
| AI file drop | `C:\Users\harsh\Downloads\AII-DUMP` |
| AI bridge script | `C:\Users\harsh\Downloads\AII-DUMP\BRIDGE\apply_patch.py` |
| Framework | .NET 10 + .NET Aspire AppHost SDK 9.3.1 |
| Runtime | `net10.0` across ALL projects |
| Shell | PowerShell (Windows) + Bash (WSL2) |
| IDE | Visual Studio 2022/2026 |
| Package management | Central Package Management — `Directory.Packages.props` |
| Container runtime | Docker + Minikube (namespace: `ums`) |
| Orchestration | Kubernetes with Kustomize (`k8s/base` + `k8s/overlays/dev-local`) |

---

## 2. SOLUTION STRUCTURE

```
AspireApp1/
├── UMS.slnx                          ← Solution file (use this, not AspireApp1.slnx)
├── Directory.Packages.props          ← ALL NuGet versions — NEVER add Version= to .csproj
├── Directory.Build.props             ← _backups excluded from build; MimeKit audit suppress
├── aegis.config.json                 ← Aegis linter config (AGS-006 disabled)
├── src/
│   ├── AppHost/                      ← Aspire orchestrator (SDK 9.3.1)
│   ├── ServiceDefaults/              ← Shared: OTel, Serilog, health, resilience, EF Core
│   ├── Shared/UMS.SharedKernel/      ← Domain primitives (see Section 5)
│   ├── ApiGateway/                   ← YARP + JWT + ServiceDefaults ✅ (fixed)
│   ├── BFF/                          ← Backend-for-Frontend
│   ├── Web/                          ← Blazor UI (Redis output cache)
│   ├── Cli/Ums.Cli/                  ← Architecture CLI tool (ums scan / snapshot)
│   ├── Governance/Aegis.Core/        ← Roslyn Clean Architecture linter
│   └── Services/
│       ├── Academic/
│       ├── Attendance/
│       ├── Examination/
│       ├── Faculty/
│       ├── Fee/
│       ├── Hostel/
│       ├── Identity/
│       ├── Notification/
│       └── Student/
├── src/Tests/
│   ├── AppHost.IntegrationTests/
│   ├── Identity.IntegrationTests/
│   ├── Kafka.IntegrationTests/
│   └── TenantIsolation.Tests/        ← Uses real Postgres (no Testcontainers)
├── src/.ums/snapshots/               ← Aegis baseline snapshots (committed)
└── k8s/
    ├── base/                         ← All services + infra + monitoring (Prometheus + Grafana)
    └── overlays/dev-local/           ← Patches: REGION_ID=dev-local, REGION_ROLE=PRIMARY
```

---

## 3. SERVICE ANATOMY — ALL 9 SERVICES ARE IDENTICAL

```
Services/{Name}/
├── {Name}.Domain/          ← Entities, Value Objects, Domain Events, Exceptions
├── {Name}.Application/     ← CQRS commands/queries, validators, interfaces
├── {Name}.Infrastructure/  ← EF Core, Kafka, repos, OutboxRelayService
├── {Name}.API/             ← Endpoints, middleware, DI wiring, Dockerfile
└── {Name}.Tests/           ← xUnit unit tests
```

**Dependency rule (enforced by Aegis):**
```
Domain        ← zero external project dependencies
Application   ← Domain only
Infrastructure ← Application (+ Domain transitively)
API           ← Application + Infrastructure (wiring only)
```

---

## 4. SHAREDKERNEL — ACTUAL CONTENTS

Located at `src/Shared/UMS.SharedKernel/`

### Domain Layer

```csharp
// BaseEntity — ALL entities inherit this
public abstract class BaseEntity
{
    public Guid           Id        { get; protected set; } = Guid.NewGuid();
    public Guid           TenantId  { get; protected set; }
    public DateTimeOffset CreatedAt { get; protected set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; protected set; } = DateTimeOffset.UtcNow;
    public uint           RowVersion { get; private set; }  // optimistic concurrency
    protected void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}

// AggregateRoot — entities that own domain events
public abstract class AggregateRoot : BaseEntity, IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();
    protected void RaiseDomainEvent(IDomainEvent e) => _domainEvents.Add(e);
}

// DomainEvent — base for all domain events (abstract)
public abstract class DomainEvent : IDomainEvent, ITenantedEvent
{
    public Guid           EventId    { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
    public abstract Guid  TenantId   { get; }   // must be overridden
}

// OutboxMessage — stored in EACH service's DB, relayed by OutboxRelayServiceBase
public sealed class OutboxMessage
{
    public Guid            Id             { get; init; } = Guid.NewGuid();
    public string          EventType      { get; init; } = string.Empty;
    public string          Payload        { get; init; } = string.Empty;
    public string?         TenantId       { get; init; }        // string, not Guid
    public DateTimeOffset  OccurredAt     { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset  CreatedAt      { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAt    { get; set; }
    public int             RetryCount     { get; set; }
    public string?         DeadLetteredAt { get; set; }
    public string?         Error          { get; set; }

    public static OutboxMessage Create(string eventType, string payload, Guid tenantId = default)
        => new() { EventType = eventType, Payload = payload, TenantId = tenantId.ToString() };
}
```

### Exceptions

```csharp
// Domain exceptions implement IDomainException (interface, not base class)
public interface IDomainException
{
    string Code    { get; }
    string Message { get; }
}

// GlobalExceptionMiddleware maps codes to HTTP status:
// DomainExceptionCodes.NotFound  → 404
// DomainExceptionCodes.Conflict  → 409
// Code starts with "INVALID_"    → 422
// Other IDomainException         → 400
// ArgumentException              → 422
// KeyNotFoundException           → 404
// Unhandled                      → 500
```

### Tenancy

```csharp
public interface ITenantContext
{
    Guid   TenantId   { get; }   // throws if not resolved
    string Slug       { get; }
    string Tier       { get; }   // e.g. "standard", "premium"
    bool   IsResolved { get; }
}
// TenantContext.SetTenant() throws if called twice — immutable once set
```

### Kafka

```csharp
// KafkaTopics — ALWAYS use these constants, never hardcode topic strings
KafkaTopics.IdentityEvents     = "identity-events"
KafkaTopics.StudentEvents      = "student-events"
KafkaTopics.AcademicEvents     = "academic-events"
KafkaTopics.AttendanceEvents   = "attendance-events"
KafkaTopics.ExaminationEvents  = "examination-events"
KafkaTopics.FeeEvents          = "fee-events"
KafkaTopics.FacultyEvents      = "faculty-events"
KafkaTopics.HostelEvents       = "hostel-events"
KafkaTopics.NotificationEvents = "notification-events"

// KafkaEventEnvelope — standard wire format for all events
public sealed class KafkaEventEnvelope
{
    public Guid     EventId       { get; init; }
    public string   EventType     { get; init; }
    public string   TenantId      { get; init; }   // string
    public string   RegionOrigin  { get; init; }   // e.g. "dev-local"
    public DateTime OccurredAt    { get; init; }   // DateTime UTC (NOT DateTimeOffset)
    public string   SchemaVersion { get; init; } = "1.0";
    public string   Payload       { get; init; }   // nested JSON
}

// KafkaConsumerBase<TEvent> — REQUIRES REGION_ID in configuration
// Group ID format: {serviceName}.{regionId}.{purpose}
// Throws InvalidOperationException on startup if REGION_ID is missing
```

### Infrastructure Bases

```csharp
// OutboxRelayServiceBase<TDbContext> — extend once per service
// Polls every 5s, batch size 50, publishes to Kafka topic
// Must override: protected abstract string TopicName { get; }

// DomainEventDispatcherInterceptorBase — EF SaveChanges interceptor
// Automatically dispatches domain events via MediatR after SaveChangesAsync
// Attach to DbContext in AddDbContext() options
```

### Application

```csharp
// ValidationBehavior<TRequest, TResponse>  (American spelling — no 'u')
// Runs all IValidator<TRequest> before handler
// Register: cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>))
```

---

## 5. ACTUAL KNOWN VIOLATIONS (as of 2026-03-14)

| # | Status | Issue |
|---|---|---|
| 1 | ✅ Fixed | `k8s/base/secret.yaml` had real credentials |
| 2 | ✅ Fixed | Identity.Domain/Application referenced EF Identity package |
| 3 | ❌ Open | `Faculty.API.csproj` + `Student.API.csproj` have `Confluent.Kafka` in API layer |
| 4 | ✅ Fixed | `ApiGateway` missing `ServiceDefaults` |
| 5 | ❌ Open | `Identity.API.csproj` still has `Confluent.Kafka` directly in API layer |
| 6 | ❌ Open | `binDebug/` from Identity service is committed to git — should be gitignored |

---

## 6. CI/CD — THREE SEPARATE WORKFLOWS

The pipeline is NOT a single 7-stage file. It's three chained workflows:

### Workflow 1: `ci.yml` — triggered on push to main/feature/**, PRs

```
detect-changes (paths-filter — per service + shared)
     │
     ▼
build (dotnet build UMS.slnx --configuration Release)
     │
     ▼
unit-tests (9 services in parallel, change-aware, skips unchanged)
     │  Coverage threshold: 70% (enforced, CI fails below)
     ▼
integration-tests (TenantIsolation.Tests, real Postgres service container)
     │  Only runs on: main branch, PRs, or shared/* changes
     ▼
test-summary (dorny/test-reporter + ReportGenerator coverage merge)
```

### Workflow 2: `docker-build.yml` — triggered on CI success on main only

```
gate (abort if CI failed)
     │
     ▼
detect-changes (re-runs paths-filter on triggering commit)
     │
     ▼
build-push (11 images in parallel, change-aware, GHA cache per service)
     │  Tags: <sha8> + latest → ghcr.io/harshmaury/ums/<service>
     ▼
collect-digests (merges all image-digest artifacts into one)
```

### Workflow 3: `deploy.yml` — triggered on Docker Build success on main

```
gate (abort if Docker Build failed, exports SHA + tag)
     │
     ▼
update-manifests (yq patches image tags in k8s YAML → commits [skip ci])
     │  Runs on: ubuntu-latest (NOT self-hosted)
     ▼
deploy (runs-on: self-hosted WSL2 runner)
     │  1. minikube image load all 11 images
     │  2. minikube addons enable metrics-server
     │  3. Ensure 9 Kafka topics exist
     │  4. kubectl apply -k k8s/overlays/dev-local
     │  5. Smoke tests: all 10 /health endpoints via api-gateway
     │  6. Update Aegis baseline snapshot → commits [skip ci]
     ▼
notify-failure (creates GitHub issue if deploy fails)
```

**Branch behaviour:**
- `feature/**` push → `ci.yml` stages 1-4 only (Docker Build not triggered)
- `main` push → full chain: ci → docker-build → deploy
- PR to main → `ci.yml` only

---

## 7. RUNNING LOCALLY (Aspire — fastest for dev)

```powershell
$Repo = "C:\Users\harsh\source\repos\AspireApp1"

# Kill any stale dotnet processes first
Get-Process -Name "dotnet","BFF","dcp","dcpctrl","dcpproc" -ErrorAction SilentlyContinue | Stop-Process -Force

# Start Aspire (no Docker, no Minikube needed)
dotnet run --project "$Repo\src\AppHost\AspireApp1.AppHost.csproj" --launch-profile https
```

Aspire dashboard opens in browser. Hot-reload active. All 9 services + Kafka + Postgres + Seq spin up locally.

---

## 8. RUNNING IN KUBERNETES (Minikube, WSL2)

```bash
dev start       # Minikube + pods + Kafka topics + port-forwards + runner
dev watch       # Auto-deploy on .cs file save (~30s rebuild cycle)
dev status      # Live dashboard
dev recovery    # Auto-fix crashed pods, missing topics, dropped port-forwards
```

**Service names for dev commands:**
```
identity-api  academic-api  student-api  attendance-api
examination-api  fee-api  faculty-api  hostel-api
notification-api  api-gateway  bff
```

---

## 9. ACCESS URLS

| Service | URL | Notes |
|---|---|---|
| API Gateway | http://localhost:8080 | Main entry |
| API Gateway Health | http://localhost:8080/health | All services proxied |
| BFF | http://localhost:5001 | |
| Identity API | http://localhost:5002 | Token endpoint |
| Seq | http://localhost:8081 | Structured logs |
| Jaeger | http://localhost:16686 | Distributed traces |
| Prometheus | (port-forward) | Metrics scraping |
| Grafana | (port-forward) | Dashboards — provisioned ✅ |

---

## 10. BUILD & TEST COMMANDS

```powershell
$Repo = "C:\Users\harsh\source\repos\AspireApp1"

# Build entire solution
dotnet build "$Repo\UMS.slnx" --no-incremental

# Build single service
dotnet build "$Repo\src\Services\Academic\Academic.API" --no-incremental

# Test single service
dotnet test "$Repo\src\Services\Academic\Academic.Tests" --logger "console;verbosity=minimal"

# Integration tests (needs Postgres running via Aspire or Docker)
dotnet test "$Repo\src\Tests\TenantIsolation.Tests"

# Architecture scan
dotnet run --project "$Repo\src\Cli\Ums.Cli" -- scan --solution UMS.slnx

# Architecture snapshot (after intentional change)
dotnet run --project "$Repo\src\Cli\Ums.Cli" -- govern snapshot create baseline --project .
```

---

## 11. EF CORE MIGRATIONS

```powershell
$Svc  = "Academic"  # Change per service
$Repo = "C:\Users\harsh\source\repos\AspireApp1"
Set-Location "$Repo\src\Services\$Svc\$Svc.Infrastructure"
dotnet ef migrations add <MigrationName> --startup-project "..\$Svc.API"
dotnet ef database update               --startup-project "..\$Svc.API"
```

---

## 12. AI FILE EXCHANGE (AII-Bridge)

All AI-generated files must use this naming convention:

```
[PROJECT]__[OBJECT]__[ACTION]__[TARGET]__[YYYYMMDDTHHMMSS]__[TRACEID].[EXT]
```

Example:
```
UMS__FILE__PATCH__IDENTITY_DOMAIN_EVENTS__20260314T160000__A3F1.cs
UMS__ZIP__REFACTOR__SHARED_KERNEL__20260314T170000__C5D8.zip
```

**Apply a generated file:**
```powershell
cd C:\Users\harsh\Downloads\AII-DUMP\BRIDGE
python apply_patch.py UMS          # review + confirm
python apply_patch.py UMS --auto   # skip confirmation
```

**Upload project context to Claude:**
```powershell
$Svc = "Academic"
$Repo = "C:\Users\harsh\source\repos\AspireApp1"
$Out  = "C:\Users\harsh\Downloads\AII-DUMP\UMS__ZIP__CONTEXT__${Svc}__$(Get-Date -Format 'yyyyMMddTHHmmss')__A0B1.zip"
$Tmp  = "$env:TEMP\ums-ctx"
Remove-Item $Tmp -Recurse -Force -ErrorAction SilentlyContinue
Copy-Item "$Repo\src\Services\$Svc" $Tmp -Recurse -Force
Get-ChildItem $Tmp -Recurse -Directory |
  Where-Object { $_.Name -in @("bin","obj",".vs","binDebug","binRelease") } |
  Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
Compress-Archive "$Tmp\*" $Out -Force
Remove-Item $Tmp -Recurse -Force
Write-Host "Ready: $Out"
```

---

## 13. PACKAGE RULES

- **NEVER** add `Version="..."` to any `PackageReference` in `.csproj`
- **ALWAYS** add new packages to `Directory.Packages.props` first
- ASP.NET Core types → `<FrameworkReference Include="Microsoft.AspNetCore.App" />`
- EF Core → Infrastructure only (`{Name}.Infrastructure.csproj`)
- `Confluent.Kafka` → Infrastructure only (violations exist in Faculty.API + Student.API + Identity.API — backlog)
- `MediatR` → Application + Infrastructure + API (not Domain)

---

## 14. SECURITY RULES

1. `k8s/base/secret.yaml` — empty placeholders only, real values in `ums-secrets.env.local` (gitignored)
2. `ums-secrets.env.local` — gitignored, never commit
3. `_backups/` — gitignored (Directory.Build.props excludes from build)
4. `binDebug/` — must be gitignored (**currently partially committed in Identity service — needs cleanup**)
5. Connection strings — always from `IConfiguration`, never hardcoded
6. `REGION_ID` — required env var for all Kafka consumers; injected by k8s overlay

---

## 15. AEGIS LINTER CONFIG (aegis.config.json)

```json
{
  "failLevel": "Error",
  "disabledRules": ["AGS-006"],
  "excludedPaths": ["_backups/"],
  "severityOverrides": {
    "AGS-008": "Info",
    "AGS-014": "Info",
    "AGS-015": "Info"
  },
  "sharedKernelAssemblies": ["UMS.SharedKernel"]
}
```

---

## 16. GITHUB ACTIONS RUNNER

| Property | Value |
|---|---|
| Location | `~/actions-runner/` |
| Name | `wsl2-minikube` |
| Labels | `self-hosted, linux, minikube` |
| Service | `actions.runner.Harshmaury-AspireApp1.wsl2-minikube` |

```bash
sudo systemctl status  actions.runner.Harshmaury-AspireApp1.wsl2-minikube
sudo systemctl restart actions.runner.Harshmaury-AspireApp1.wsl2-minikube
journalctl -u actions.runner.Harshmaury-AspireApp1.wsl2-minikube -f
```

---

## 17. TROUBLESHOOTING

| Symptom | Fix |
|---|---|
| `dev` not found | `source ~/.bashrc` |
| Port-forwards dropped | `dev start` |
| Pod CrashLoopBackOff | `dev logs <svc>` → `dev recovery` |
| Kafka consumer crash on startup | Check `REGION_ID` is set in configmap overlay |
| Kafka topics missing | `dev recovery` |
| identity-api crashing | Check `OpenIddict__SigningKeyXml` in `ums-secrets` |
| Build fails: wrong solution file | Use `UMS.slnx`, not `AspireApp1.slnx` |
| CI coverage gate fails | Line coverage below 70% threshold |
| Deploy: runner offline | `sudo systemctl restart actions.runner...` |
| Minikube won't start | `minikube delete && minikube start --driver=docker --cpus=4 --memory=8192` |
| After minikube delete | `kubectl apply -k k8s/overlays/dev-local` then `dev start` |

---

## 18. WHAT WAS OUTDATED IN PREVIOUS DOCS

| Item | Old (wrong) | Actual |
|---|---|---|
| Solution file | `AspireApp1.slnx` | `UMS.slnx` |
| AI drop folder | `ums-drop` | `AII-DUMP` (Chrome configured) |
| CI pipeline | "7 stages in ci.yml" | 3 separate workflows (ci/docker-build/deploy) |
| ApiGateway ServiceDefaults | "missing (backlog #4)" | Fixed — present in csproj |
| Monitoring (Grafana/Prometheus) | "pending" | Deployed — in kustomization.yaml |
| `OutboxMessage.TenantId` type | `Guid` | `string?` |
| `OutboxMessage.OccurredAt` type | `DateTime` | `DateTimeOffset` |
| `BaseEntity.CreatedAt` type | `DateTime` | `DateTimeOffset` |
| Domain exception type | `DomainException : Exception` | `IDomainException` interface |
| `ValidationBehaviour` spelling | British 'u' | American: `ValidationBehavior` |
| `ITenantContext` members | `TenantId`, `Slug` only | + `Tier`, `IsResolved` |
| Kafka consumer group format | undocumented | `{service}.{regionId}.{purpose}` (KAFKA-001) |
| `REGION_ID` requirement | not mentioned | Required env var — consumers throw without it |
| Codegen file format | `[project]__[service]__[feature]__[YYYYMMDD_HHMM].[ext]` | AII-Bridge 7-token format |
