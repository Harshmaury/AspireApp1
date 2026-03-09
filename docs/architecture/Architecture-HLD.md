# Architecture — High Level Design (HLD)
## University Management System (UMS)
> .NET 10 · Aspire · Kubernetes (Minikube) · Kafka · PostgreSQL

---

## 1. System Overview

UMS is a **multi-tenant, microservices-based** University Management System built on .NET 10. It manages the full lifecycle of a university — students, faculty, academics, attendance, examinations, fees, hostels, and notifications — with a shared Identity and authentication backbone.

The system is designed around:
- **Clean Architecture** — strict 4-layer separation per service (Domain → Application → Infrastructure → API)
- **Domain-Driven Design** — aggregates, domain events, bounded contexts
- **CQRS + MediatR** — commands and queries routed through a mediator pipeline
- **Event-Driven Architecture** — Kafka outbox pattern for inter-service communication
- **Multi-tenancy** — all services enforce tenant isolation via `TenantContext` and global EF Core query filters
- **Governance-as-Code** — Aegis rule engine enforces architectural constraints at CI time

---

## 2. Component Architecture

```
┌──────────────────────────────────────────────────────────────────┐
│                        Client Layer                              │
│            Web App / Mobile / External Consumers                 │
└──────────────────────────┬───────────────────────────────────────┘
                           │ HTTPS
┌──────────────────────────▼───────────────────────────────────────┐
│                   Ingress (Nginx / Traefik)                      │
│               TLS termination · Host routing                     │
└────────────┬──────────────────────────┬──────────────────────────┘
             │                          │
┌────────────▼──────────┐  ┌────────────▼────────────────────────┐
│   BFF (Port 5001)     │  │   API Gateway / YARP (Port 8080)    │
│  ASP.NET Minimal API  │  │   Reverse proxy · JWT forwarding    │
│  Dashboard, Profile   │  │   Route: /identity /student …       │
└────────────┬──────────┘  └─────┬──────┬──────┬──────┬──────────┘
             └───────────────────┘      │      │      │
              ┌─────────────────────────┘      │      └──────────────────┐
              │                                │                         │
┌─────────────▼──────┐  ┌──────────────────────▼──────┐  ┌─────────────▼──────┐
│  Identity.API      │  │  Academic.API / Student.API  │  │  Notification.API  │
│  :5002             │  │  Faculty.API / Attendance    │  │  :5010             │
│  OpenIddict Auth   │  │  Examination / Fee / Hostel  │  │  Email / SMS       │
└─────────┬──────────┘  └────────────┬────────────────┘  └─────────┬──────────┘
          │  (each service)          │                              │
┌─────────▼──────────┐  ┌────────────▼─────────────┐  ┌────────────▼──────────┐
│  PostgreSQL (own)  │  │   Kafka (shared broker)   │  │  PostgreSQL (own)     │
│  per-service DB    │  │   9 topics · 3 partitions │  │  per-service DB       │
│  EF Core + Npgsql  │  │   Outbox pattern          │  │  EF Core + Npgsql     │
└────────────────────┘  └───────────────────────────┘  └───────────────────────┘

┌──────────────────────────────────────────────────────────────────┐
│                    Observability Stack                           │
│  Seq (structured logs)  ·  Jaeger (traces)  ·  Prometheus       │
│  Grafana dashboards  ·  OpenTelemetry SDK (all services)        │
└──────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────┐
│                    Governance Layer                              │
│  Aegis.Core (rule engine)  ·  Ums.Cli (CLI runner)              │
│  Rules: layer matrix, domain isolation, tenant isolation,        │
│  circular dependencies, cross-service references, Kafka topics,  │
│  event schema compatibility, resilience, logging contracts       │
└──────────────────────────────────────────────────────────────────┘
```

---

## 3. Service Architecture

### 3.1 Nine Domain Services

| Service | Port | Domain | Kafka Topic | DB |
|---------|------|--------|-------------|-----|
| Identity | 5002 | Auth, Users, Tenants | `identity-events` | identity_db |
| Academic | 5004 | Courses, Programmes, Curricula, Departments | `academic-events` | academic_db |
| Student | 5003 | Student lifecycle, enrollment | `student-events` | student_db |
| Attendance | 5005 | Records, summaries, condonation | `attendance-events` | attendance_db |
| Examination | 5006 | Schedules, marks, hall tickets, results | `examination-events` | examination_db |
| Fee | 5007 | Structures, payments, scholarships | `fee-events` | fee_db |
| Faculty | 5008 | Faculty members, publications, course assignments | `faculty-events` | faculty_db |
| Hostel | 5009 | Rooms, allotments, complaints | `hostel-events` | hostel_db |
| Notification | 5010 | Email/SMS dispatch, templates, preferences | `notification-events` | notification_db |

### 3.2 Platform Components

| Component | Role |
|-----------|------|
| ApiGateway | YARP reverse proxy, JWT claim forwarding, route `/service/…` to backend |
| BFF | Aggregated dashboard and profile endpoints for frontend consumers |
| AppHost | .NET Aspire 10 orchestration for local development |
| ServiceDefaults | Shared OTEL, Serilog, health checks, resilience defaults |
| SharedKernel | `BaseEntity`, `IAggregateRoot`, `DomainEvent`, `OutboxMessage`, `PagedResult` |
| Aegis.Core | Architecture rule engine |
| Ums.Cli | CLI interface: `govern verify`, `govern snapshot`, AI commands, context commands |

---

## 4. Infrastructure Architecture

```
Kubernetes Namespace: ums
┌────────────────────────────────────────────────────────────────────┐
│  Workloads (Deployments)                                          │
│  ┌──────────────────────────────────────────────────────────────┐ │
│  │  9 service pods + api-gateway + bff = 11 deployments        │ │
│  │  HPA on identity-api (CPU-based autoscaling)                 │ │
│  └──────────────────────────────────────────────────────────────┘ │
│  Stateful / Infrastructure                                        │
│  ┌──────────────────────────────────────────────────────────────┐ │
│  │  postgres        StatefulSet  (PVC: postgres-pvc)            │ │
│  │  kafka           Deployment   (depends on zookeeper)         │ │
│  │  zookeeper       Deployment                                  │ │
│  │  seq             Deployment   (PVC: seq-pvc)                 │ │
│  │  jaeger          Deployment                                  │ │
│  │  prometheus      Deployment   (PVC, RBAC)                    │ │
│  │  grafana         Deployment   (PVC, configmap dashboards)    │ │
│  └──────────────────────────────────────────────────────────────┘ │
│  Networking                                                       │
│  ┌──────────────────────────────────────────────────────────────┐ │
│  │  Ingress (ingress.yaml) routes external traffic              │ │
│  │  ClusterIP services for all internal pods                    │ │
│  └──────────────────────────────────────────────────────────────┘ │
│  Config & Secrets                                                 │
│  ┌──────────────────────────────────────────────────────────────┐ │
│  │  configmap-base.yaml   → shared env (Kafka, Seq, Jaeger)     │ │
│  │  secret.yaml           → connection strings, OpenIddict keys │ │
│  └──────────────────────────────────────────────────────────────┘ │
│  Kustomize Overlays                                               │
│  ┌──────────────────────────────────────────────────────────────┐ │
│  │  k8s/base/            → canonical manifests                  │ │
│  │  k8s/overlays/dev-local/ → dev overrides (configmap patch)   │ │
│  └──────────────────────────────────────────────────────────────┘ │
└────────────────────────────────────────────────────────────────────┘
```

---

## 5. Deployment Architecture

```
Developer Machine (Windows)
│
├── Visual Studio 2022/2026
│   └── .NET Aspire (local dev mode, no Docker required)
│
└── WSL2
    ├── Minikube (Docker driver, 4 CPU / 8 GB)
    │   └── Namespace: ums
    │       └── All pods + infra
    │
    └── GitHub Actions Self-Hosted Runner
        └── Picks up CI/CD jobs from GitHub
            ├── Build → Test → Docker Build
            └── Deploy → Minikube via kubectl

GitHub Actions (ubuntu-latest cloud runners for CI)
└── Self-hosted runner (WSL2) for Deploy stage only
```

---

## 6. Event Architecture

Inter-service communication is entirely asynchronous via Kafka. No service makes direct HTTP calls to another service (governance rule enforced).

```
Producer Services          Kafka Topics                Consumer Services
─────────────────          ────────────                ─────────────────
Identity      ──────────►  identity-events   ──────►  Notification
Student       ──────────►  student-events    ──────►  Notification
                                             ──────►  Attendance (enrollment)
Academic      ──────────►  academic-events   ──────►  Notification
                                             ──────►  Examination (course pub)
Fee           ──────────►  fee-events        ──────►  Notification
Examination   ──────────►  examination-events ─────►  Notification
Faculty       ──────────►  faculty-events    ──────►  (internal)
Hostel        ──────────►  hostel-events     ──────►  (internal)
Attendance    ──────────►  attendance-events ──────►  (internal)
Notification  ──────────►  notification-events ────►  (audit/log)
```

All producers use the **Transactional Outbox Pattern**: domain events are persisted atomically in the same DB transaction as business data, then a background `OutboxRelayService` polls and publishes to Kafka.

---

## 7. Multi-Tenancy Architecture

```
HTTP Request
    │
    ▼
TenantMiddleware  ← reads X-Tenant-ID header or JWT claim
    │
    ▼
TenantContext (scoped DI)  ← holds TenantId for request lifetime
    │
    ▼
EF Core Global Query Filter  ← WHERE tenant_id = @tenantId on every query
    │
    ▼
Domain Entities (BaseEntity has TenantId)
```

Every aggregate inherits `BaseEntity` which carries `TenantId`. Tenant provisioning is handled exclusively by the Identity service. Other services receive tenant context via header.

---

## 8. Security Architecture

| Layer | Mechanism |
|-------|-----------|
| Authentication | OpenIddict (OAuth 2.0 password flow + refresh tokens) |
| Token | JWT, signed with RSA key stored in K8s secret |
| Authorization | JWT claims forwarded by API Gateway |
| Multi-tenancy | Tenant ID validated on every request |
| Secrets | K8s Secrets (`ums-secrets`) |
| Container security | Trivy scanning in CI (CRITICAL/HIGH reported to Security tab) |
| Governance | Aegis rules block cross-service direct references |

---

## 9. Observability Architecture

| Signal | Tool | Integration |
|--------|------|-------------|
| Structured logs | Seq | Serilog → OTLP → Seq |
| Distributed traces | Jaeger | OpenTelemetry SDK → OTLP |
| Metrics | Prometheus + Grafana | Prometheus scrape → Grafana dashboards |
| Health checks | ASP.NET Core health endpoints | `/health` on every service |
| CI test results | TRX → dorny/test-reporter | GitHub Actions step summary |
| Coverage | ReportGenerator | Cobertura XML → GitHub step summary |

---

## 10. Local Development Architecture

```
.NET Aspire AppHost (Windows)
├── Registers all 11 services
├── Provides service discovery (no manual port config)
├── Hot reload per service
└── Aspire Dashboard (resource viewer, logs, traces)

dev CLI (WSL2 Bash)
├── dev start      → Minikube + port-forwards + self-hosted runner
├── dev deploy     → Docker build → minikube image load → kubectl rollout
├── dev watch      → inotifywait → auto-deploy on file change
└── dev status     → live pod/service dashboard
```
