# UMS WORKFLOW

**University Management System — AI-Assisted Development Protocol**

.NET 10 · Aspire · Kubernetes · Kafka · PostgreSQL · Clean Architecture

| Property | Value |
|---|---|
| Developer | Harsh Maury |
| Project | UMS (github.com/Harshmaury/AspireApp1) |
| Drop Zone | `C:\Users\harsh\Downloads\ums-drop` |
| Version | 1.0.0 |
| Updated | 2026-03-12 |

---

## Overview

This document defines the working protocol between **Harsh** and **Claude** for all UMS development. Every task is tracked by a **Unique Key**. Files are delivered through the **Drop Zone**. Claude requests exactly what it needs — nothing more. Harsh drops exactly what was requested — nothing else.

This workflow is designed for a 9-service microservice system with strict Clean Architecture enforcement. It scales from single-file hotfixes to multi-service refactors.

---

## 1. Unique Key System

Every unit of work — a bug fix, a feature, a refactor, a config change — is identified by a **Unique Key**.

### 1.1 Key Format

```
UMS-<LAYER>-<PRIORITY>-<SEQ>
```

| Segment | Values | Meaning |
|---|---|---|
| `LAYER` | `SVC`, `INFRA`, `K8S`, `CI`, `GOV`, `SEC`, `SHARED`, `BFF`, `GW` | Which layer/area |
| `PRIORITY` | `P0`, `P1`, `P2`, `P3` | Urgency (P0 = critical blocker) |
| `SEQ` | `001`–`999` | Sequential counter per layer |

### 1.2 Examples

| Key | Meaning |
|---|---|
| `UMS-SHARED-P0-001` | SharedKernel EF Core build failure — critical |
| `UMS-SEC-P0-001` | Secret.yaml purged from git — critical security |
| `UMS-K8S-P0-001` | K8s folder restructure — deploy pipeline blocker |
| `UMS-K8S-P0-002` | Image name standardisation across all deployments |
| `UMS-CI-P1-001` | Governance baselines committed |
| `UMS-SVC-P1-001` | MimeKit CVE fix in Notification service |
| `UMS-SVC-P2-001` | Identity service feature work |

### 1.3 Service Codes (for SVC layer)

When the key targets a specific domain service, append the service code:

```
UMS-SVC-<PRIORITY>-<SEQ>-<SVCCODE>
```

| Service Code | Service |
|---|---|
| `IDN` | Identity |
| `ACA` | Academic |
| `STU` | Student |
| `ATT` | Attendance |
| `EXM` | Examination |
| `FEE` | Fee |
| `FAC` | Faculty |
| `HST` | Hostel |
| `NTF` | Notification |
| `BFF` | BFF |
| `GW` | ApiGateway |

**Example:** `UMS-SVC-P1-001-NTF` = Notification service, priority 1, first task.

---

## 2. Drop Zone Protocol

The drop zone is the **single handoff point** between Harsh and Claude.

```
C:\Users\harsh\Downloads\ums-drop\
```

### 2.1 How It Works

```
Step 1: Harsh describes the task or problem in chat
Step 2: Claude assigns a Unique Key and lists EXACTLY which files it needs
Step 3: Harsh drops only those files into ums-drop\
Step 4: Harsh types: "dropped <KEY>" in chat
Step 5: Claude reads, implements, produces output
Step 6: Claude replies with: changes made, files to copy back, commands to run
Step 7: Harsh applies changes, runs verification
Step 8: Harsh confirms or reports errors
Step 9: Key is marked DONE ✅ or a follow-up key is opened
```

### 2.2 Drop Folder Structure

Organize drops by key so nothing gets mixed up:

```
ums-drop\
  UMS-SHARED-P0-001\
    UMS.SharedKernel.csproj
  UMS-SEC-P0-001\
    secret.yaml
    .gitignore
  UMS-K8S-P0-001\
    (all flat yaml files from repo root)
  UMS-CI-P1-001\
    governance.yml
    ci.yml
```

> **Rule:** Never mix files from different keys in the same drop. One folder per key.

### 2.3 What Claude Will Never Ask For

Claude will never ask you to drop:
- The entire solution or all source files at once
- Files unrelated to the active key
- Binary files (`.dll`, `.exe`, `.nupkg`) unless explicitly needed for inspection
- `.git` folder or history

### 2.4 File Request Format

When Claude requests files, it will always use this format:

```
📥 FILES NEEDED — UMS-K8S-P0-001

Drop these into: ums-drop\UMS-K8S-P0-001\

  [ ] k8s/*.yaml                    — all flat manifest files at repo root
  [ ] .github/workflows/deploy.yml  — deploy workflow
  [ ] kustomization.yaml            — if it exists at root

When ready, type: dropped UMS-K8S-P0-001
```

---

## 3. Command Reference

Type these commands in chat at any time:

| Command | What Happens |
|---|---|
| `new <description>` | Claude assigns a Unique Key, scopes the work, lists files needed |
| `dropped <KEY>` | Claude processes the dropped files and begins work |
| `status` | Claude lists all open keys and their current state |
| `done <KEY>` | Marks a key as complete, updates the changelog |
| `context <KEY>` | Claude summarises what it knows about a key and what's left |
| `diff <KEY>` | Claude shows a summary of all changes made under this key |
| `block <KEY> <reason>` | Marks a key as blocked — recorded with reason |
| `unblock <KEY>` | Resumes a blocked key |
| `rollback <KEY>` | Claude produces the reverse patch/commands to undo the key's changes |
| `audit` | Claude reviews all P0/P1 keys and reports overall project health |

---

## 4. Response Format

Every Claude response to a `dropped <KEY>` command follows this structure:

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
KEY:    UMS-SHARED-P0-001
TITLE:  SharedKernel EF Core missing package reference
STATUS: ✅ COMPLETE
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

ANALYSIS
────────
<What Claude found in the dropped files>

CHANGES
───────
<Exact file changes — diffs, new content, or commands>

COPY BACK
─────────
<Which files to copy from drop output back to repo, and where>

RUN
───
<Commands to run after copying — build, test, verify>

VERIFY
──────
<What success looks like — expected output>

NEXT
────
<Follow-up keys to open, if any>
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

---

## 5. Layer Architecture Reference

Claude enforces Clean Architecture at every layer. This is the dependency rule for every service:

```
Domain
  └── no dependencies (pure C# — aggregates, value objects, domain events)

Application
  └── depends on: Domain only
  └── contains: commands, queries, MediatR handlers, validators

Infrastructure
  └── depends on: Domain + Application interfaces
  └── contains: EF Core, Kafka producers/consumers, repositories

API
  └── depends on: Application (via MediatR dispatch)
  └── DI registration wires Infrastructure
  └── Minimal API endpoints only — no business logic
```

**Aegis enforcement:** `LayerMatrixRule` and `DomainIsolationRule` run on every `governance.yml` push. CI fails on violation. Claude will never suggest a change that breaks these rules.

### 5.1 Multi-Tenancy Rule

Every entity that touches user data **must**:
- Inherit `TenantId` from `BaseEntity`
- Have a global EF Core query filter on `TenantId`
- Never hardcode tenant resolution — always use injected `ITenantContext`

Claude will flag any dropped code that violates this.

### 5.2 Kafka Outbox Rule

Domain events are **never** published directly to Kafka. The pattern is:

```
SaveChanges()
  → DomainEventDispatcherInterceptorBase dispatches in-process events
  → OutboxMessage written to outbox table (same transaction)
  → OutboxRelayServiceBase polls and publishes to Kafka
```

Claude will never suggest bypassing this pattern.

---

## 6. Active Key Backlog

Current open keys as of `2026-03-12`:

### P0 — Critical Blockers

| Key | Title | Status | Est. Time |
|---|---|---|---|
| `UMS-SHARED-P0-001` | SharedKernel: Add EF Core package reference | 🔴 OPEN | 30 min |
| `UMS-SEC-P0-001` | Purge secret.yaml from git + Sealed Secrets | 🔴 OPEN | 2–4 hr |
| `UMS-K8S-P0-001` | Restructure K8s folder to Kustomize hierarchy | 🔴 OPEN | 1–2 hr |
| `UMS-K8S-P0-002` | Standardise image names across all deployments | 🔴 OPEN | 30 min |

### P1 — High Priority

| Key | Title | Status | Est. Time |
|---|---|---|---|
| `UMS-GOV-P1-001` | Commit governance baselines + event-schemas dir | 🟡 OPEN | 30 min |
| `UMS-K8S-P1-001` | Apply ConfigMap real connection string patches | 🟡 OPEN | 45 min |
| `UMS-SVC-P1-001-NTF` | Update MimeKit 4.10.0 → 4.11.0 (CVE fix) | 🟡 OPEN | 15 min |
| `UMS-SVC-P1-002` | Update KubernetesClient 16.0.7 → 16.1.0 (CVE fix) | 🟡 OPEN | 15 min |

### P2 — Normal

| Key | Title | Status | Est. Time |
|---|---|---|---|
| `UMS-CI-P2-001` | Add deployment approval gate (GitHub Environments) | 🔵 OPEN | 20 min |
| `UMS-K8S-P2-001` | Verify BFF vs ApiGateway deployment.yaml mismatch | 🔵 OPEN | 30 min |
| `UMS-INFRA-P2-001` | Nexus .nexus.yaml + K8s provider integration | 🔵 OPEN | 1 hr |

---

## 7. Key Lifecycle

```
OPEN  →  IN PROGRESS  →  DONE ✅
          ↓
        BLOCKED  →  UNBLOCKED  →  IN PROGRESS
          ↓
        CANCELLED ❌
```

A key moves to `IN PROGRESS` the moment Harsh types `dropped <KEY>`.  
A key is `DONE` when Harsh confirms the verify step passes.  
A key is `BLOCKED` if it depends on another open key.

### 7.1 Dependency Chain

```
UMS-SHARED-P0-001
  └── must complete before → all 9 service builds
        └── must complete before → UMS-K8S-P0-002 (image standardisation)
              └── must complete before → docker-build.yml unblocks
                    └── must complete before → security.yml unblocks
                          └── must complete before → deploy.yml can run

UMS-SEC-P0-001 (independent — run in parallel with build fix)

UMS-K8S-P0-001
  └── must complete before → UMS-K8S-P1-001 (configmap patches)
        └── must complete before → deploy.yml works end-to-end

UMS-GOV-P1-001
  └── must complete before → governance.yml drift check stops skipping
```

---

## 8. Code Generation Rules

When Claude produces code for UMS, it follows these rules without exception:

### 8.1 Naming Conventions

| Artifact | Convention | Example |
|---|---|---|
| Commands | `<Verb><Entity>Command` | `EnrollStudentCommand` |
| Queries | `Get<Entity>Query` | `GetStudentByIdQuery` |
| Handlers | `<Command/Query>Handler` | `EnrollStudentCommandHandler` |
| Domain Events | `<Entity><PastTense>Event` | `StudentEnrolledEvent` |
| Kafka Topics | `<service>-events` | `student-events` |
| K8s resources | `<service>-api` | `student-api` |
| GHCR images | `ghcr.io/harshmaury/ums/<service>-api:<sha8>` | `ghcr.io/harshmaury/ums/student-api:a1b2c3d4` |
| DB names | `<service>_db` | `student_db` |

### 8.2 File Header

Every new `.cs` file Claude produces includes:

```csharp
// UMS — University Management System
// Key: <UNIQUE-KEY>
// Service: <ServiceName>
// Layer: <Domain|Application|Infrastructure|API>
```

### 8.3 Entity Baseline

Every new entity must extend `BaseEntity` from `SharedKernel`:

```csharp
public class MyEntity : BaseEntity
{
    // TenantId inherited — do not redeclare
    // Id inherited as Guid
}
```

### 8.4 EF Core Global Filter (mandatory)

Every `DbContext` must include the tenant filter for every entity:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<MyEntity>()
        .HasQueryFilter(e => e.TenantId == _tenantContext.TenantId);
}
```

---

## 9. Verification Checklist

After every key completes, confirm the following depending on layer:

### Build verification
```bash
dotnet build UMS.slnx -c Release
# Expected: Build succeeded. 0 Error(s)
```

### Governance verification
```bash
dotnet run --project src/Cli/Ums.Cli -c Release -- govern verify all --project .
# Expected: All rules PASS
```

### K8s verification
```bash
kubectl apply -k k8s/overlays/dev-local --dry-run=client
# Expected: No errors
```

### Security verification
```bash
dotnet list package --vulnerable --include-transitive
# Expected: No vulnerable packages
```

---

## 10. Nexus Integration Context

UMS is controlled by **Nexus** (github.com/Harshmaury/Nexus) via its Kubernetes provider. Nexus scales Deployment replicas up/down — no UMS code changes required.

`.nexus.yaml` at the UMS repo root registers all services. Key: `UMS-INFRA-P2-001` covers creating this file.

Nexus CLI commands once provider is built:
```bash
engx register /mnt/c/Users/harsh/source/repos/AspireApp1
engx start ums
engx stop ums
engx status ums
```

---

## 11. Changelog

| Date | Key | Change |
|---|---|---|
| 2026-03-12 | — | v1.0.0 — Workflow document created. 11 active keys defined across P0–P2. |

---

## 12. Quick Reference Card

```
START A TASK:   "new <what you want to do>"
DROP FILES:     "dropped <KEY>"
CHECK STATUS:   "status"
FINISH A TASK:  "done <KEY>"
GET CONTEXT:    "context <KEY>"

KEY FORMAT:     UMS-<LAYER>-<PRIORITY>-<SEQ>
                UMS-SVC-<PRIORITY>-<SEQ>-<SVCCODE>

DROP ZONE:      C:\Users\harsh\Downloads\ums-drop\<KEY>\

P0 KEYS NOW:
  UMS-SHARED-P0-001  →  fix build (30 min)
  UMS-SEC-P0-001     →  purge secrets (urgent)
  UMS-K8S-P0-001     →  fix k8s structure (1-2 hr)
  UMS-K8S-P0-002     →  fix image names (30 min)
```
