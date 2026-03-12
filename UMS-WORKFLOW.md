# UMS WORKFLOW
**University Management System — AI Session Protocol v3.0**

.NET 10 · Aspire · Kubernetes · Kafka · PostgreSQL · Clean Architecture

| Property     | Value                                          |
|---|---|
| Developer    | Harsh Maury                                    |
| Repo         | github.com/Harshmaury/AspireApp1               |
| Drop Zone    | `C:\Users\harsh\Downloads\ums-drop\`           |
| WSL Drop     | `/mnt/c/Users/harsh/Downloads/ums-drop/`       |
| Session File | `UMS-WORKFLOW.md` (repo root)                  |
| Version      | 3.0.0                                          |
| Updated      | 2026-03-12                                     |

---

## THE PROTOCOL — How Every Session Works

### You start a session
Paste build errors OR type a command. I figure out what's needed.

### I tell you exactly what to zip
I give you one PowerShell command. You run it. One zip lands in ums-drop.

### You confirm the drop
Type: `dropped <KEY>`

### I produce a fix script
I output one `.ps1` file. You run it. Files are written directly into the repo.
No manual copying. No unzipping. Script verifies itself.

### You verify
Run `dotnet build` or the verification command I give. Paste the result.

### Done
Type `done <KEY>`. I mark it closed and update WORKFLOW.md.

---

## COMMANDS

```
status              → all open keys + their state
done <KEY>          → close a completed key
block <KEY> <why>   → mark blocked with reason
context <KEY>       → full context for one key
audit               → full health report of the repo
rollback <KEY>      → I produce a rollback .ps1
```

Anything else (errors, descriptions, questions) → I figure out the key and action.

---

## ZIP NAMING — Inbound (You → Me)

```
UMS-<KEY>-IN-<YYYYMMDD-HHMM>.zip
```

**Examples:**
```
UMS-TEST-P0-001-IN-20260312-1430.zip       ← files Claude asked for
UMS-K8S-P1-002-IN-20260312-0900.zip
```

**Rules:**
- Contains ONLY the files I asked for — nothing else
- Folder structure matches repo root (e.g. `src\Tests\...`)
- Created by the PowerShell command I give you

---

## SCRIPT NAMING — Outbound (Me → You)

```
UMS-<KEY>-FIX-<YYYYMMDD-HHMM>.ps1
```

**Examples:**
```
UMS-TEST-P0-001-FIX-20260312-1445.ps1
UMS-K8S-P1-002-FIX-20260312-0930.ps1
```

**Every fix script:**
- Has a header: key, description, files changed, how to verify
- Creates backups before overwriting (`_backups\<KEY>\`)
- Writes files using `Set-Content` with exact paths
- Ends with a verification command you can paste directly

---

## KEY FORMAT

```
UMS-<LAYER>-<PRIORITY>-<SEQ>[-<SVCCODE>]
```

### Layers

| Code     | Scope                                      |
|---|---|
| `SHARED` | UMS.SharedKernel — base classes, contracts |
| `SVC`    | A specific domain service                  |
| `INFRA`  | Cross-cutting infrastructure               |
| `K8S`    | Kubernetes manifests + Kustomize           |
| `CI`     | GitHub Actions workflows                   |
| `GOV`    | Aegis governance + Ums.Cli                 |
| `SEC`    | Security — secrets, certs, scanning        |
| `REPO`   | Repository hygiene, naming, structure      |
| `TEST`   | Test projects                              |
| `BFF`    | BFF project                                |
| `GW`     | ApiGateway project                         |

### Priorities

| Code | Meaning                          |
|---|---|
| `P0` | Build broken — fix before anything else |
| `P1` | Architecture / correctness issue        |
| `P2` | Quality, warnings, hygiene              |
| `P3` | Nice to have                            |

### Service Codes

| Code  | Service      | Code  | Service      |
|---|---|---|---|
| `IDN` | Identity     | `ACA` | Academic     |
| `STU` | Student      | `ATT` | Attendance   |
| `EXM` | Examination  | `FEE` | Fee          |
| `FAC` | Faculty      | `HST` | Hostel       |
| `NTF` | Notification |       |              |

### Examples

```
UMS-TEST-P0-001          ← Kafka integration test build failures
UMS-K8S-P1-001           ← K8s manifest image name mismatch
UMS-SVC-P1-002-IDN       ← Identity service architecture fix
UMS-SEC-P0-001           ← secret.yaml exposed in git
UMS-CI-P1-001            ← CI workflow broken
```

---

## THE ZIP COMMAND I GIVE YOU

Every time I need files, I give you an exact PowerShell block like this:

```powershell
# UMS-TEST-P0-001 — collect files Claude needs
$key  = "UMS-TEST-P0-001"
$drop = "C:\Users\harsh\Downloads\ums-drop"
$repo = "C:\Users\harsh\source\repos\AspireApp1"
$ts   = Get-Date -Format "yyyyMMdd-HHmm"
$zip  = "$drop\$key-IN-$ts.zip"

$files = @(
    "src\Tests\Kafka.IntegrationTests\Tests\AcademicOutboxRelayTests.cs",
    "src\Tests\Kafka.IntegrationTests\Tests\StudentOutboxRelayTests.cs",
    "src\Tests\Kafka.IntegrationTests\Kafka.IntegrationTests.csproj"
)

Compress-Archive -Path ($files | ForEach { "$repo\$_" }) -DestinationPath $zip -Force
Write-Host "Dropped: $zip"
```

Then you paste the result of that, or just type `dropped UMS-TEST-P0-001`.

---

## THE FIX SCRIPT I PRODUCE

Every fix I produce looks like this:

```powershell
<#
  UMS-TEST-P0-001-FIX-20260312-1445.ps1
  Fix: Kafka integration tests — Academic.Domain.Common namespace
  Files changed: 5
  Verify: dotnet build src\Tests\Kafka.IntegrationTests -c Release 2>&1 | Select-String "error|succeeded"
#>

$repo = "C:\Users\harsh\source\repos\AspireApp1"

# ── Backup ────────────────────────────────────────────────────────────────────
$backupDir = "$repo\_backups\UMS-TEST-P0-001"
New-Item -ItemType Directory -Force -Path $backupDir | Out-Null
Copy-Item "$repo\src\Tests\Kafka.IntegrationTests\Tests\AcademicOutboxRelayTests.cs" $backupDir -Force

# ── Fix ───────────────────────────────────────────────────────────────────────
Set-Content "$repo\src\Tests\Kafka.IntegrationTests\Tests\AcademicOutboxRelayTests.cs" -Encoding UTF8 @'
<fixed file content here>
'@

# ── Verify ────────────────────────────────────────────────────────────────────
Write-Host "`n=== Verifying ===" -ForegroundColor Cyan
dotnet build "$repo\src\Tests\Kafka.IntegrationTests" -c Release 2>&1 |
    Select-String "error|succeeded|FAILED"
```

---

## BUILD STATUS

```
## [UMS_BUILD_STATUS]
# Last verified: 2026-03-12 — dotnet build FAILED
# Errors remaining: 7 (6 × CS0234 in Kafka.IntegrationTests + 1 × NETSDK1022 in Identity.API)

P0 — Build Blockers:
  [ ] UMS-TEST-P0-001   Kafka.IntegrationTests — *.Domain.Common namespace missing (5 files, 5 errors)
  [ ] UMS-SVC-P0-001    Identity.API — NETSDK1022 duplicate Content items (bin/ nested in bin/)

P1 — Architecture:
  [ ] UMS-SEC-P1-001    secret.yaml committed to git (credentials exposed — purge + rotate)
  [ ] UMS-K8S-P1-001    Image names in manifests don't match CI push tags
  [ ] UMS-CI-P1-001     CI workflows reference incorrect image names + missing secrets

P2 — Quality:
  [ ] UMS-REPO-P2-001   _backups/ and .vs/ should be in .gitignore
  [ ] UMS-REPO-P2-002   KubernetesClient 16.0.7 vulnerability — upgrade to 16.0.8+

---
Done:
  [x] UMS-SHARED-P0-001  SharedKernel build failure — EF Core missing from csproj
  [x] UMS-SHARED-P0-002  BaseEntity / AggregateRoot / IDomainEvent — OutboxRelay TenantId bug
  [x] UMS-SHARED-P0-003  OutboxRelayService implementations — R1/R2/R3 iterations
```

---

## ARCHITECTURE REFERENCE

### Services (9 domain APIs)

| Service      | Port  | DB Schema    | Kafka Topics (publishes)              |
|---|---|---|---|
| Identity     | 5001  | identity     | user.created, user.updated            |
| Student      | 5002  | student      | student.enrolled, student.updated     |
| Academic     | 5003  | academic     | course.created, grade.published       |
| Attendance   | 5004  | attendance   | attendance.recorded                   |
| Examination  | 5005  | examination  | exam.scheduled, result.published      |
| Fee          | 5006  | fee          | fee.charged, payment.received         |
| Faculty      | 5007  | faculty      | faculty.assigned                      |
| Hostel       | 5008  | hostel       | room.allocated                        |
| Notification | 5009  | notification | (consumer only)                       |

### Platform Components

| Component    | Role                                        |
|---|---|
| ApiGateway   | YARP reverse proxy — single ingress point   |
| BFF          | Aggregation layer for frontend              |
| AppHost      | .NET Aspire orchestrator (local dev only)   |
| ServiceDefaults | Shared Aspire middleware (health, OTEL)  |
| SharedKernel | Base classes — Entity, Outbox, Kafka, EF    |
| Aegis        | Governance engine — drift + contract checks |
| Ums.Cli      | Developer CLI for governance commands       |

### Clean Architecture (per service)

```
<Service>.Domain          → Entities, events, interfaces (no deps)
<Service>.Application     → CQRS handlers, validators, DTOs
<Service>.Infrastructure  → EF Core, Kafka, Postgres implementations
<Service>.API             → Minimal API endpoints, DI wiring
```

### K8s Structure

```
k8s/
  base/
    namespace.yaml
    configmap-base.yaml
    secret.yaml              ← ⚠️ MUST be removed from git
    services/                ← 9 domain service deployments + services
    infra/                   ← postgres, kafka, zookeeper, seq, jaeger, monitoring
    gateway/                 ← ApiGateway deployment + HPA
    bff/                     ← BFF deployment
    kustomization.yaml
  overlays/
    dev-local/
      kustomization.yaml
      configmap-patch.yaml
```

---

## CHANGELOG

```
2026-03-12 | [WORKFLOW]  | v3.0 — rebuilt from scratch, PowerShell fix scripts, targeted zips
2026-03-12 | [BUILD]     | UMS-SHARED-P0-001/002/003 complete — SharedKernel green
2026-03-12 | [BUILD]     | Remaining blockers: TEST-P0-001 (CS0234) + SVC-P0-001 (NETSDK1022)
```
