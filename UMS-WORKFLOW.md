# UMS WORKFLOW

**University Management System — AI-Assisted Development Protocol**

.NET 10 · Aspire · Kubernetes · Kafka · PostgreSQL · Clean Architecture

| Property | Value |
|---|---|
| Developer | Harsh Maury |
| Repo | github.com/Harshmaury/AspireApp1 |
| Drop Zone | `C:\Users\harsh\Downloads\ums-drop\` |
| Version | 2.0.0 |
| Updated | 2026-03-12 |

---

## How This Works

Every unit of work has a **Unique Key**. Claude asks for exactly the files it needs. You drop them. You type `dropped <KEY>`. Claude reads, implements, tells you exactly what to copy back and what commands to run. You verify. Done.

```
new <description>       → Claude assigns key, lists files needed
dropped <KEY>           → Claude implements, produces output
status                  → all open keys + state
done <KEY>              → close a key
context <KEY>           → what Claude knows, what's left
block <KEY> <reason>    → mark blocked
rollback <KEY>          → undo a key's changes
audit                   → full project health report
```

---

## Key Format

```
UMS-<LAYER>-<PRIORITY>-<SEQ>[-<SVCCODE>]
```

| Layer | Scope |
|---|---|
| `SHARED` | UMS.SharedKernel project |
| `SVC` | A specific domain service |
| `INFRA` | Cross-cutting infrastructure |
| `K8S` | Kubernetes manifests |
| `CI` | GitHub Actions workflows |
| `GOV` | Aegis governance + Ums.Cli |
| `SEC` | Security — secrets, scanning |
| `REPO` | Repository hygiene |
| `TEST` | Test projects |
| `BFF` | BFF project |
| `GW` | ApiGateway project |

| Service Code | Service | Service Code | Service |
|---|---|---|---|
| `IDN` | Identity | `ACA` | Academic |
| `STU` | Student | `ATT` | Attendance |
| `EXM` | Examination | `FEE` | Fee |
| `FAC` | Faculty | `HST` | Hostel |
| `NTF` | Notification | | |

---

## Full Architecture Audit — 2026-03-12

Complete findings from full tree analysis. Every item mapped to a unique key.

---

### CRITICAL — Build Blockers

#### `UMS-SHARED-P0-001` — SharedKernel missing EF Core package reference
**File:** `src/Shared/UMS.SharedKernel/UMS.SharedKernel.csproj`  
`DomainEventDispatcherInterceptorBase` and `OutboxRelayServiceBase` were moved into SharedKernel but `Microsoft.EntityFrameworkCore` was never added as a `<PackageReference>`. 22 CS0246/CS0234 errors cascade into all 9 services.  
**Fix:** `dotnet add src/Shared/UMS.SharedKernel/UMS.SharedKernel.csproj package Microsoft.EntityFrameworkCore`  
**Status:** 🔴 OPEN

---

### CRITICAL — Security

#### `UMS-SEC-P0-001` — secret.yaml committed + exists in _backups
**COMPROMISED CREDENTIALS**  
`k8s/base/secret.yaml` contains base64 OpenIddict signing/encryption keys + 18 DB connection strings. Worse: `_backups/phase6-security-20260310-183925/k8s/base/secret.yaml` is a second copy. Purging only the main path is not enough — `_backups/` must be removed from full git history too.  
**Fix:** Purge both paths with `git-filter-repo`, rotate all credentials, replace with Sealed Secrets. See `_ROTATE-CREDENTIALS.md`.  
**Status:** 🔴 OPEN

---

### CRITICAL — Design Violations

#### `UMS-SHARED-P0-002` — SharedKernel missing BaseEntity
**Architecture contract broken.**  
`Student.Domain/Common/BaseEntity.cs` defines its own `BaseEntity` with `TenantId`. SharedKernel has `IAggregateRoot.cs` and `Domain/OutboxMessage.cs` but **no `BaseEntity`**. The documented contract says "every entity inherits TenantId from BaseEntity from SharedKernel" — but SharedKernel doesn't have one. Tenant isolation is being enforced inconsistently across services.  
**Fix:** Add `src/Shared/UMS.SharedKernel/Domain/BaseEntity.cs` with `Id (Guid)` + `TenantId (Guid)`.  
**Status:** 🔴 OPEN

#### `UMS-SHARED-P0-003` — Domain primitives duplicated across all 9 services
**Violates DRY. SharedKernel is being ignored.**  
Every service has its own `Domain/Common/` folder with local copies:

| Type | Duplicated In |
|---|---|
| `AggregateRoot.cs` | Academic, Attendance, Examination, Faculty, Fee, Hostel, Notification |
| `OutboxMessage.cs` | Academic, Attendance, Examination, Faculty, Fee, Hostel, Notification |
| `IAggregateRoot.cs` | Academic, Student |
| `IDomainEvent.cs` | Academic |
| `DomainEvent.cs` | Student |
| `BaseEntity.cs` | Student |

SharedKernel already defines `IAggregateRoot`, `OutboxMessage`, `DomainEventDispatcherInterceptorBase`. Per-service copies shadow or diverge from SharedKernel's versions, making Aegis governance unreliable.  
**Fix:** Consolidate all domain primitives into SharedKernel. Delete all per-service `Domain/Common/` copies.  
**Depends on:** `UMS-SHARED-P0-002`  
**Status:** 🔴 OPEN

#### `UMS-SVC-P0-001-STU` — OutboxRelayService in wrong layer (Student)
**Clean Architecture violation.**  
`src/Services/Student/Student.API/Services/StudentOutboxRelayService.cs` — API layer contains infrastructure logic (DB polling + Kafka publish). Aegis `LayerMatrixRule` will flag this.  
**Fix:** Move to `Student.Infrastructure/Services/StudentOutboxRelayService.cs`. Remove from API project.  
**Status:** 🔴 OPEN

#### `UMS-SVC-P0-002-FAC` — OutboxRelayService in wrong layer (Faculty)
**Clean Architecture violation.**  
`src/Services/Faculty/Faculty.API/Services/FacultyOutboxRelayService.cs` — same violation as Student.  
**Fix:** Move to `Faculty.Infrastructure/Services/FacultyOutboxRelayService.cs`. Remove from API project.  
**Status:** 🔴 OPEN

---

### HIGH — Repository Hygiene

#### `UMS-REPO-P1-001` — _backups/ committed to git
Manual phase snapshots have no place in a git repo — that's what git history is for. Also contains the compromised secret copy from `UMS-SEC-P0-001`.  
**Fix:** Remove from history with `git-filter-repo`. Add `_backups/` to `.gitignore`. **Run alongside `UMS-SEC-P0-001`** as a single `git-filter-repo` operation covering all three paths.  
**Status:** 🟡 OPEN

#### `UMS-REPO-P1-002` — binDebug/ compiled output committed to git
`src/Services/Identity/Identity.Infrastructure/binDebug/` — full .NET build output including DLLs, EXEs, and localized resources for 10 languages. Happened because `.gitignore` excludes `bin/` but folder was named `binDebug/`.  
**Fix:** Remove from history in the same `git-filter-repo` pass. Add `binDebug/` and `objDebug/` to `.gitignore`.  
**Status:** 🟡 OPEN

#### `UMS-REPO-P1-003` — .bak files throughout source tree (20+ files)
```
.github/workflows/deploy.yml.bak
src/Cli/Ums.Cli/Adapters/VerifyBoundariesAdapter.cs.bak
src/Cli/Ums.Cli/Adapters/VerifyDependenciesAdapter.cs.bak
src/Cli/Ums.Cli/Adapters/VerifyRegionAdapter.cs.bak
src/Cli/Ums.Cli/Adapters/VerifyResilienceAdapter.cs.bak
src/Cli/Ums.Cli/Adapters/VerifyTenantAdapter.cs.bak
src/Cli/Ums.Cli/Commands/GitCommands.cs.bak2
src/Cli/Ums.Cli/Commands/GovernCommands.cs.bak
src/Governance/Aegis.Core/Building/LayerClassifier.cs.bak
src/Services/Faculty/.../DomainEventDispatcherInterceptor.cs.bak2
src/Tests/TenantIsolation.Tests/Helpers/DbFactory.cs.bak_* (×8)
aegis.config.json.bak  |  Directory.Build.props.bak
Directory.Packages.props.bak  |  Script/TenantIsolationRule.cs.bak_*
```
**Fix:** Delete all `.bak`/`.bak2`/`.bak_*` files. Add `*.bak`, `*.bak[0-9]*`, `*.bak_*` to `.gitignore`.  
**Status:** 🟡 OPEN

#### `UMS-REPO-P1-004` — Script/ folder at repo root
`Script/` contains iterative debugging artefacts: `DbFactory-v4-FINAL.cs`, `DbFactory-v5-FINAL.cs`, `DbFactory.cs`, `test-output.txt`, `dbfactory-fix.txt`, `setup-identity-tests.ps1`, `TenantIsolationRule.cs`, `ags007-output/`. No production function.  
**Fix:** Delete `Script/` entirely.  
**Status:** 🟡 OPEN

#### `UMS-REPO-P1-005` — Debug dump files inside Student service
`src/Services/Student/student_full_dump.txt` and `student_structure_map.txt` — text dumps committed inside the source tree.  
**Fix:** Delete both files.  
**Status:** 🟡 OPEN

#### `UMS-REPO-P1-006` — Two solution files at repo root
Both `AspireApp1.slnx` (original scaffold name) and `UMS.slnx` (renamed) exist at root. Ambiguous for CI and contributors.  
**Fix:** Delete `AspireApp1.slnx`. Verify all CI yml files reference `UMS.slnx`.  
**Status:** 🟡 OPEN

---

### HIGH — Architecture Violations

#### `UMS-INFRA-P1-001` — TenantMiddleware duplicated per service
```
src/ServiceDefaults/TenantMiddleware.cs            ← canonical
src/Services/Academic/Academic.API/Middleware/TenantMiddleware.cs
src/Services/Attendance/Attendance.API/Middleware/TenantMiddleware.cs
src/Services/Faculty/Faculty.API/Middleware/TenantMiddleware.cs
```
ServiceDefaults exists so shared middleware is not duplicated. Per-service copies will drift when the canonical version is updated.  
**Fix:** Delete per-service copies. All services use `TenantMiddleware` from `ServiceDefaults`.  
**Status:** 🟡 OPEN

#### `UMS-INFRA-P1-002` — TenantContext duplicated between ServiceDefaults and SharedKernel
```
src/ServiceDefaults/TenantContext.cs                     ← concrete
src/Shared/UMS.SharedKernel/Tenancy/TenantContext.cs     ← also concrete
src/Shared/UMS.SharedKernel/Tenancy/ITenantContext.cs    ← interface (correct home)
```
`ITenantContext` correctly belongs in SharedKernel. The concrete `TenantContext` should exist only in `ServiceDefaults`. Having two concrete implementations means services may wire the wrong one.  
**Fix:** Remove `src/Shared/UMS.SharedKernel/Tenancy/TenantContext.cs`. Keep only `ITenantContext` in SharedKernel. Single concrete `TenantContext` in ServiceDefaults.  
**Status:** 🟡 OPEN

#### `UMS-INFRA-P1-003` — MigrationHostedService duplicated within ServiceDefaults
```
src/ServiceDefaults/MigrationHostedService.cs
src/ServiceDefaults/Extensions/MigrationHostedService.cs
```
Two files, same class, same project. One shadows the other.  
**Fix:** Delete root-level copy. Keep `Extensions/MigrationHostedService.cs`.  
**Status:** 🟡 OPEN

#### `UMS-INFRA-P1-004` — Duplicate empty Migrations/ folders (Attendance, Faculty)
```
Attendance.Infrastructure\Migrations\          ← populated (authoritative)
Attendance.Infrastructure\Persistence\Migrations\  ← empty

Faculty.Infrastructure\Migrations\             ← populated (authoritative)
Faculty.Infrastructure\Persistence\Migrations\     ← empty
```
Empty duplicates confuse EF Core design-time tooling and `dotnet ef migrations add` targeting.  
**Fix:** Delete the empty `Persistence/Migrations/` folders in both services.  
**Status:** 🟡 OPEN

#### `UMS-INFRA-P1-005` — Empty Controllers/ folders in all 9 Minimal API services
Every service API project has an empty `Controllers/` folder — VS MVC scaffold remnants. UMS uses Minimal API (`Endpoints/` pattern). These mislead contributors into placing MVC controllers.  
**Fix:** Delete all 9 empty `Controllers/` folders.  
**Status:** 🟡 OPEN

#### `UMS-INFRA-P1-006` — Leftover Aspire template projects (ApiService + Web)
```
src/Services/ApiService/   → AspireApp1.ApiService (weather scaffold)
src/Web/                   → Blazor weather app (Aspire scaffold)
```
Default Aspire template projects never removed. No UMS domain function. They add noise to the solution build and CI change-detection matrix.  
**Fix:** Delete both project folders from disk. Remove from `UMS.slnx`. Update CI `detect-changes` job.  
**Status:** 🟡 OPEN

---

### HIGH — Governance & CVE

#### `UMS-GOV-P1-001` — Governance baselines missing
`src/.ums/event-schemas/` does not exist. `governance.yml` drift check and event contract verification silently skip every run.  
**Fix:** Run `govern snapshot create baseline` once build is clean. Create `event-schemas/` dir. Commit both.  
**Depends on:** `UMS-SHARED-P0-001`  
**Status:** 🟡 OPEN

#### `UMS-SVC-P1-001-NTF` — MimeKit CVE GHSA-g7hc-96xr-gvvx
MimeKit `4.10.0` in Notification.API, Notification.Infrastructure, AppHost.IntegrationTests.  
**Fix:** Update to `4.11.0+`  
**Status:** 🟡 OPEN

#### `UMS-SVC-P1-002` — KubernetesClient CVE GHSA-w7r3-mgwf-4mqq
KubernetesClient `16.0.7` in AppHost.  
**Fix:** Update to `16.1.0+`  
**Status:** 🟡 OPEN

---

### NORMAL — Pipeline, K8s, Config

#### `UMS-K8S-P2-001` — Image names mismatch
Deployment YAMLs reference `aspireapp1-<service>:v1.0.0`. CI pushes `ghcr.io/harshmaury/ums/<service>-api:<sha8>`. Deploy will pull stale/wrong images.  
**Fix:** Update all deployment YAMLs to `ghcr.io/harshmaury/ums/<service>-api:latest`, `imagePullPolicy: Always`.  
**Status:** 🔵 OPEN

#### `UMS-K8S-P2-002` — ConfigMap __PATCH_REQUIRED__ sentinels not filled
`k8s/base/configmap-base.yaml` has `__PATCH_REQUIRED__` placeholders for all DB connection strings. `dev-local` overlay only adds `REGION_*` values — no overlay fills real connection strings.  
**Fix:** Add strategic merge patch in `dev-local/kustomization.yaml`.  
**Status:** 🔵 OPEN

#### `UMS-K8S-P2-003` — BFF vs ApiGateway deployment content to verify
`k8s/base/bff/` and `k8s/base/gateway/` now exist as separate folders. Content needs drop-and-verify pass to confirm correct image references and ports.  
**Status:** 🔵 OPEN

#### `UMS-CI-P2-001` — No deployment approval gate
`deploy.yml` auto-deploys to Minikube on every main push. Zero human gate.  
**Fix:** Add GitHub Environment `dev-local` with required reviewer. Reference in `deploy.yml`.  
**Status:** 🔵 OPEN

#### `UMS-INFRA-P2-001` — Create .nexus.yaml + Nexus K8s provider
Register all 9 services + postgres StatefulSet with Nexus for `engx start/stop ums`.  
**Status:** 🔵 OPEN

---

### NORMAL — Test Hygiene

#### `UMS-TEST-P2-001` — UnitTest1.cs boilerplate in Aegis.Tests
`src/Governance/Aegis.Tests/UnitTest1.cs` — VS default test scaffold, never populated.  
**Fix:** Delete.  
**Status:** 🔵 OPEN

#### `UMS-TEST-P2-002` — TestResults/*.trx files committed
Every test project has `TestResults/ags007-results.trx` committed. Test run artefacts should never be in source control.  
**Fix:** Add `**/TestResults/` and `**/*.trx` to `.gitignore`. Remove from git tracking.  
**Status:** 🔵 OPEN

---

## Full Key Backlog

### P0 — Critical Blockers

| Key | Title | Status | Est. |
|---|---|---|---|
| `UMS-SHARED-P0-001` | SharedKernel: Add EF Core package ref | 🔴 OPEN | 15 min |
| `UMS-SHARED-P0-002` | SharedKernel: Add BaseEntity | 🔴 OPEN | 30 min |
| `UMS-SHARED-P0-003` | Consolidate Domain primitives into SharedKernel | 🔴 OPEN | 2 hr |
| `UMS-SEC-P0-001` | Purge secret.yaml + _backups from git history | 🔴 OPEN | 2–4 hr |
| `UMS-SVC-P0-001-STU` | Move StudentOutboxRelayService to Infrastructure | 🔴 OPEN | 20 min |
| `UMS-SVC-P0-002-FAC` | Move FacultyOutboxRelayService to Infrastructure | 🔴 OPEN | 20 min |

### P1 — High Priority

| Key | Title | Status | Est. |
|---|---|---|---|
| `UMS-REPO-P1-001` | Remove _backups/ from git history | 🟡 OPEN | 30 min |
| `UMS-REPO-P1-002` | Remove binDebug/ from git | 🟡 OPEN | 15 min |
| `UMS-REPO-P1-003` | Delete all .bak files + .gitignore rules | 🟡 OPEN | 20 min |
| `UMS-REPO-P1-004` | Delete Script/ folder | 🟡 OPEN | 10 min |
| `UMS-REPO-P1-005` | Delete Student debug dump .txt files | 🟡 OPEN | 5 min |
| `UMS-REPO-P1-006` | Delete AspireApp1.slnx (keep UMS.slnx only) | 🟡 OPEN | 10 min |
| `UMS-INFRA-P1-001` | Remove per-service TenantMiddleware copies | 🟡 OPEN | 30 min |
| `UMS-INFRA-P1-002` | Remove duplicate TenantContext from SharedKernel | 🟡 OPEN | 20 min |
| `UMS-INFRA-P1-003` | Remove duplicate MigrationHostedService | 🟡 OPEN | 10 min |
| `UMS-INFRA-P1-004` | Delete empty duplicate Migrations/ folders | 🟡 OPEN | 15 min |
| `UMS-INFRA-P1-005` | Delete empty Controllers/ folders from all APIs | 🟡 OPEN | 10 min |
| `UMS-INFRA-P1-006` | Remove ApiService + Web scaffold projects | 🟡 OPEN | 20 min |
| `UMS-GOV-P1-001` | Commit governance baselines + event-schemas dir | 🟡 OPEN | 30 min |
| `UMS-SVC-P1-001-NTF` | MimeKit CVE 4.10.0 → 4.11.0 | 🟡 OPEN | 15 min |
| `UMS-SVC-P1-002` | KubernetesClient CVE 16.0.7 → 16.1.0 | 🟡 OPEN | 15 min |

### P2 — Normal

| Key | Title | Status | Est. |
|---|---|---|---|
| `UMS-K8S-P2-001` | Standardise image names to ghcr.io scheme | 🔵 OPEN | 30 min |
| `UMS-K8S-P2-002` | Fill ConfigMap __PATCH_REQUIRED__ sentinels | 🔵 OPEN | 30 min |
| `UMS-K8S-P2-003` | Verify BFF vs ApiGateway deployment content | 🔵 OPEN | 20 min |
| `UMS-CI-P2-001` | Add deployment approval gate | 🔵 OPEN | 20 min |
| `UMS-INFRA-P2-001` | Create .nexus.yaml + Nexus K8s provider | 🔵 OPEN | 1 hr |
| `UMS-TEST-P2-001` | Delete UnitTest1.cs from Aegis.Tests | 🔵 OPEN | 5 min |
| `UMS-TEST-P2-002` | Remove TestResults/*.trx + .gitignore rules | 🔵 OPEN | 10 min |

---

## Execution Order

```
PHASE 0 — Security (start immediately, parallel)
  UMS-SEC-P0-001 + UMS-REPO-P1-001 + UMS-REPO-P1-002
    → single git-filter-repo run covering:
        k8s/base/secret.yaml
        _backups/
        src/Services/Identity/Identity.Infrastructure/binDebug/

PHASE 1 — Build (unblocks everything downstream)
  UMS-SHARED-P0-001  → build goes green
    └── UMS-SHARED-P0-002  → BaseEntity in SharedKernel
          └── UMS-SHARED-P0-003  → Domain primitives consolidated
                ├── UMS-SVC-P0-001-STU  → Student layers clean
                └── UMS-SVC-P0-002-FAC  → Faculty layers clean
                      → CI green → docker-build → security → deploy

PHASE 2 — Repo hygiene (single commit, no dependencies)
  UMS-REPO-P1-003 + P1-004 + P1-005 + P1-006
  UMS-TEST-P2-001 + UMS-TEST-P2-002

PHASE 3 — Infrastructure cleanup (after Phase 1)
  UMS-INFRA-P1-001 through P1-006 → single commit

PHASE 4 — K8s + CI + CVE (after Phase 1)
  UMS-SVC-P1-001-NTF + UMS-SVC-P1-002 (CVE)
  UMS-K8S-P2-001 + P2-002 + P2-003
  UMS-CI-P2-001

PHASE 5 — Governance + Nexus (requires clean build)
  UMS-GOV-P1-001
  UMS-INFRA-P2-001
```

---

## Architecture Contract

```
Domain         → no dependencies
Application    → Domain only
Infrastructure → Domain + Application interfaces
API            → Application (MediatR) + Infrastructure (DI wiring only)
```

**Tenant isolation:** Every entity extends SharedKernel `BaseEntity`. Global EF query filter on `TenantId` in every `DbContext`. Resolve via `ITenantContext` (injected) — never hardcoded.

**Kafka Outbox:** Domain events → outbox table (same transaction) → `OutboxRelayServiceBase` polls → Kafka. Never publish directly to Kafka. `OutboxRelayService` lives in **Infrastructure only**.

**Naming conventions:**

| Artifact | Pattern | Example |
|---|---|---|
| Command | `<Verb><Entity>Command` | `EnrollStudentCommand` |
| Query | `Get<Entity>Query` | `GetStudentByIdQuery` |
| Handler | `<Command/Query>Handler` | `EnrollStudentCommandHandler` |
| Domain Event | `<Entity><PastTense>Event` | `StudentEnrolledEvent` |
| Kafka Topic | `<service>-events` | `student-events` |
| K8s resource | `<service>-api` | `student-api` |
| GHCR image | `ghcr.io/harshmaury/ums/<service>-api:<sha8>` | |
| DB name | `<service>_db` | `student_db` |

**File header (all new .cs files):**
```csharp
// UMS — University Management System
// Key: <UNIQUE-KEY>
// Service: <ServiceName>
// Layer: <Domain|Application|Infrastructure|API|Shared>
```

---

## Verification Commands

```powershell
# Build
dotnet build UMS.slnx -c Release
# → Build succeeded. 0 Error(s)

# Governance
dotnet run --project src/Cli/Ums.Cli -c Release -- govern verify all --project .
# → All rules PASS

# K8s dry run
kubectl apply -k k8s/overlays/dev-local --dry-run=client
# → No errors

# CVE scan
dotnet list package --vulnerable --include-transitive
# → No vulnerable packages

# Secret scan
gitleaks detect --source . --no-git
# → 0 secrets detected
```

---

## Response Format

Every Claude response to `dropped <KEY>` follows this structure:

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
KEY:    UMS-SHARED-P0-001
TITLE:  SharedKernel EF Core missing package reference
STATUS: ✅ COMPLETE
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
ANALYSIS  — what was found in the dropped files
CHANGES   — exact diffs, new file content, or commands
COPY BACK — which files to copy back into the repo and where
RUN       — commands to execute after copying
VERIFY    — expected output confirming success
NEXT      — follow-up keys to open
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

---

## Observability

| Service | Port-forward Command | URL |
|---|---|---|
| Grafana | `kubectl port-forward -n ums service/grafana 3000:3000` | localhost:3000 |
| Prometheus | `kubectl port-forward -n ums service/prometheus 9090:9090` | localhost:9090 |
| Jaeger | `kubectl port-forward -n ums service/jaeger 16686:16686` | localhost:16686 |
| Seq | `kubectl port-forward -n ums service/seq 8081:80` | localhost:8081 |
| Aspire | `dotnet run --project src/AppHost/...` | localhost:15888 |

---

## Changelog

| Date | Version | Change |
|---|---|---|
| 2026-03-12 | 1.0.0 | Initial — 11 keys from status doc |
| 2026-03-12 | 2.0.0 | Full tree audit — 27 keys. New critical finds: missing SharedKernel BaseEntity, Domain primitives duplicated across all 9 services, OutboxRelay layer violations in Student + Faculty, binDebug compiled output in git, _backups contains second secret copy, 20+ .bak files, empty Controllers/ folders in all 9 services, duplicate TenantContext, duplicate MigrationHostedService, ApiService + Web Aspire scaffold leftovers, two solution files at root |

---

## Quick Reference Card

```
START:   new <description>
DROP:    dropped <KEY>
STATUS:  status
DONE:    done <KEY>

DROP ZONE: C:\Users\harsh\Downloads\ums-drop\<KEY>\

PHASE 0 — NOW:
  UMS-SEC-P0-001        purge secrets + _backups + binDebug (URGENT)

PHASE 1 — NEXT:
  UMS-SHARED-P0-001     fix build              (15 min)
  UMS-SHARED-P0-002     add BaseEntity         (30 min)
  UMS-SHARED-P0-003     consolidate Domain     (2 hr)
  UMS-SVC-P0-001-STU    Student layers clean   (20 min)
  UMS-SVC-P0-002-FAC    Faculty layers clean   (20 min)

PHASE 2 — THEN:
  27 total keys across P0–P2 — see backlog above
```
