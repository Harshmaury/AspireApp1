
---

## PowerShell Diagnostic Rules

### NEVER DO — Token Killers
```powershell
# NEVER — dumps hundreds of bin/obj lines
Get-ChildItem -Recurse "src\..."
# NEVER — no scope
Get-ChildItem "src" | Select-Object FullName


---

## PowerShell Diagnostic Rules

### NEVER DO — Token Killers
- NEVER use `Get-ChildItem -Recurse` without filtering bin/obj — dumps hundreds of useless lines
- NEVER read whole folder structures — always target specific known files first
- NEVER use `Select-String` across entire src without scoping to -Filter

### ALWAYS DO — Scoped Commands

#### Find a file by name (no bin/obj noise):
```powershell
Get-ChildItem `src` -Filter `*.cs` -Recurse | Where-Object { $_.FullName -notmatch `\\\\bin\\\\` -and $_.FullName -notmatch `\\\\obj\\\\` } | Select-Object FullName
```

#### Find which file contains a method/symbol:
```powershell
Get-ChildItem `src` -Filter `*.cs` -Recurse | Where-Object { $_.FullName -notmatch `\\\\bin\\\\` -and $_.FullName -notmatch `\\\\obj\\\\` } | Select-String `MethodOrSymbolName` | Select-Object Path
```


---

## PowerShell Diagnostic Rules

### NEVER DO — Token Killers
- NEVER: Get-ChildItem -Recurse without bin/obj filter — dumps hundreds of useless lines
- NEVER: Read whole folder structures — always target specific known files first
- NEVER: Select-String across entire src without -Filter scoping

### ALWAYS DO — Scoped Commands

Find file by name (clean):
  Get-ChildItem "src" -Filter "*.cs" -Recurse | Where-Object { $_.FullName -notmatch "\\bin\\" -and $_.FullName -notmatch "\\obj\\" } | Select-Object FullName

Find which file owns a symbol:
  Get-ChildItem "src" -Filter "*.cs" -Recurse | Where-Object { $_.FullName -notmatch "\\bin\\" -and $_.FullName -notmatch "\\obj\\" } | Select-String "SymbolName" | Select-Object Path

List source files in a test project only:
  Get-ChildItem "src\Tests\{Project}" -Filter "*.cs" -Recurse | Where-Object { $_.FullName -notmatch "\\bin\\" -and $_.FullName -notmatch "\\obj\\" } | Select-Object FullName

### Canonical File Paths (Never Search For These)
AppHost:           src\AppHost\AppHost.cs
AppHost csproj:    src\AppHost\AspireApp1.AppHost.csproj
Aliases:           C:\Users\harsh\source\repos\AspireApp1\ums-aliases.ps1
PS Profile:        C:\Users\harsh\Documents\PowerShell\Microsoft.PowerShell_profile.ps1
ServiceDefaults:   src\ServiceDefaults\
SharedKernel:      src\Shared\UMS.SharedKernel\
ApiGateway:        src\ApiGateway\
BFF:               src\BFF\
Service Pattern:   src\Services\{Name}\{Name}.API\Program.cs
Service csproj:    src\Services\{Name}\{Name}.API\{Name}.API.csproj
AppHost Int Tests: src\Tests\AppHost.IntegrationTests\
Test Helpers:      src\Tests\AppHost.IntegrationTests\Helpers\
Identity Int Tests:src\Tests\Identity.IntegrationTests\
TenantIsolation:   src\Tests\TenantIsolation.Tests\
Kafka Tests:       src\Tests\Kafka.IntegrationTests\

### Service File Lookup Pattern
For ANY service {Name}: Academic, Student, Fee, Faculty, Attendance, Examination, Notification, Hostel
  Program.cs:    src\Services\{Name}\{Name}.API\Program.cs
  DbContext:     src\Services\{Name}\{Name}.Infrastructure\Persistence\{Name}DbContext.cs
  Entities:      src\Services\{Name}\{Name}.Domain\Entities\
  Commands:      src\Services\{Name}\{Name}.Application\Commands\
  Queries:       src\Services\{Name}\{Name}.Application\Queries\
  Repositories:  src\Services\{Name}\{Name}.Infrastructure\Repositories\
  OutboxRelay:   src\Services\{Name}\{Name}.Infrastructure\Kafka\OutboxRelayService.cs
  Unit Tests:    src\Tests\{Name}.Tests\
EXCEPTION: Student OutboxRelay ? src\Services\Student\Student.API\Services\StudentOutboxRelayService.cs

### AppHost Integration Test Debug Pattern
Step 1: Read ONLY these 2 files first:
  Get-Content "src\Tests\AppHost.IntegrationTests\Helpers\ServiceWebFactory.cs"
  Get-Content "src\Tests\AppHost.IntegrationTests\Helpers\ServiceFixture.cs"
Step 2: Read specific failing test:
  Get-Content "src\Tests\AppHost.IntegrationTests\{Service}\{Service}IntegrationTests.cs"
Step 3: NEVER read bin/, obj/, or whole folder listings

### csproj Relative Path Table
From src\Services\{Name}\{Name}.API\  ? SharedKernel: ..\..\..\Shared\UMS.SharedKernel\UMS.SharedKernel.csproj
From src\Services\{Name}\{Name}.API\  ? ServiceDefaults: ..\..\..\ServiceDefaults\AspireApp1.ServiceDefaults.csproj
From src\ApiGateway\                  ? SharedKernel: ..\Shared\UMS.SharedKernel\UMS.SharedKernel.csproj
From src\ApiGateway\                  ? ServiceDefaults: ..\ServiceDefaults\AspireApp1.ServiceDefaults.csproj
From src\BFF\                         ? SharedKernel: ..\Shared\UMS.SharedKernel\UMS.SharedKernel.csproj
From src\BFF\                         ? ServiceDefaults: ..\ServiceDefaults\AspireApp1.ServiceDefaults.csproj
From src\AppHost\                     ? SharedKernel: ..\Shared\UMS.SharedKernel\UMS.SharedKernel.csproj
From src\Tests\{Test}\                ? SharedKernel: ..\..\Shared\UMS.SharedKernel\UMS.SharedKernel.csproj

### Required Usings (Memorised)
UseGlobalExceptionHandler() ? using UMS.SharedKernel.Extensions;
  Source: src\Shared\UMS.SharedKernel\Extensions\ExceptionMiddlewareExtensions.cs

### Duplicate Key Fix (Universal — Run Whenever DuplicateKeyInHashLiteral Appears)
$path = "C:\Users\harsh\source\repos\AspireApp1\ums-aliases.ps1"
$lines = Get-Content $path
$seen = @{}
$cleaned = foreach ($line in $lines) {
    $match = [regex]::Match($line, '"(\w+)"\s*=\s*"src/')
    if ($match.Success) {
        $key = $match.Groups[1].Value
        if (-not $seen[$key]) { $line; $seen[$key] = $true }
    } else { $line }
}
$cleaned | Set-Content $path

### Session Diagnostic Checklist (Run Every Session End)
ut-all
ub all 2>&1 | ue
dotnet test src/Tests/Identity.IntegrationTests/Identity.IntegrationTests.csproj -v normal
ut isolation
ut kafka
ut apphost
git log --oneline -5
git status

---

## Session 13 Additions (Applied Session 14)

### No RequireAuthorization on Service Endpoints (ADR-006)
# Individual services MUST NOT have .RequireAuthorization() on any endpoint.
# Gateway owns auth. Services have no auth middleware in their pipeline.
# .RequireAuthorization() without UseAuthorization() causes hard runtime exception.

### GetTenantId — Always Read from Header
// NEVER — no JWT in services:
// httpContext.User.FindFirstValue("tenant_id")

// ALWAYS — gateway forwards header:
// httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault()

### MigrateWithRetryAsync — Required Pattern
# Never use bare db.Database.Migrate() or MigrateAsync() at startup.
# Always use MigrateWithRetryAsync<TDb> local static function (see Phase 8 in Phase Plan).

### Seq Configuration Key
# "Seq__ServerUrl" (double underscore — standard ASP.NET Core config binding)
# Read in SerilogExtensions.cs: builder.Configuration["Seq:ServerUrl"]

### Jaeger — ContainerResource Wiring
# Jaeger is AddContainer() ? ContainerResource ? does NOT support .WithReference()
# Wire via .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", jaeger.GetEndpoint("otlp-grpc")) only.

### TenantMiddleware — Header Based (ADR-006)
# ServiceDefaults\TenantMiddleware.cs reads X-Tenant-Id and X-Tenant-Slug from request headers.
# NOT from JWT claims. Updated Session 13.

### AppHost csproj Correct Path
# src\AppHost\AspireApp1.AppHost.csproj  (NOT src\AppHost\AppHost.csproj)

### AppHost Integration Tests — Three-Layer Fix (Session 13)
# 1. Strip .RequireAuthorization() from all endpoint files
# 2. Fix GetTenantId to read X-Tenant-Id header
# 3. Add default headers to ServiceFixture.InitializeAsync():
#    Client.DefaultRequestHeaders.Add("X-Tenant-Id", TestTenant.Id.ToString());
#    Client.DefaultRequestHeaders.Add("X-User-Id", Guid.NewGuid().ToString());

### ServiceDefaults Package: Serilog.Sinks.Seq
# Version: 8.0.0 — added Session 13

### Observability Resources in AppHost
# var seq    = builder.AddSeq("seq").ExcludeFromManifest();
# var jaeger = builder.AddContainer("jaeger", "jaegertracing/all-in-one", "1.57")
#                     .WithEndpoint(port: 16686, targetPort: 16686, name: "ui")
#                     .WithEndpoint(port: 4317,  targetPort: 4317,  name: "otlp-grpc")
#                     .ExcludeFromManifest();
