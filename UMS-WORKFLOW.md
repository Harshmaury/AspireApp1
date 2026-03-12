# UMS WORKFLOW
**University Management System — AI Session Protocol v1.1.0**

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

                           |

### Service Codes

| Code  | Service      | Code  | Service      |
|---|---|---|---|
| `IDN` | Identity     | `ACA` | Academic     |
| `STU` | Student      | `ATT` | Attendance   |
| `EXM` | Examination  | `FEE` | Fee          |
| `FAC` | Faculty      | `HST` | Hostel       |
| `NTF` | Notification |       |              |

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
