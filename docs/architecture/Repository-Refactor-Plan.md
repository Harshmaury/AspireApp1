# Repository Improvement & Refactor Plan
## University Management System (UMS)

> Prioritized by impact vs effort. Each item includes rationale, steps, and estimated effort.

---

## Priority Legend

| Priority | Meaning |
|----------|---------|
| 🔴 Critical | Security risk or blocks reliability |
| 🟠 High | Architectural debt or pipeline risk |
| 🟡 Medium | Engineering quality / DRY improvements |
| 🟢 Low | Nice-to-have, polish |

---

## Phase 1 — Security & Reliability (Do Now)

### 🔴 1.1 Remove `secret.yaml` from Repository

**Problem:** `k8s/base/secret.yaml` contains base64-encoded connection strings and OpenIddict signing keys. Even base64 is not encryption — anyone with repo access can decode them.

**Steps:**
1. Remove `secret.yaml` from git history using `git filter-repo` or BFG Repo Cleaner
2. Rotate all secrets immediately (PostgreSQL passwords, OpenIddict signing/encryption keys)
3. Choose a secrets management approach:
   - **Simple (recommended for solo/small team):** Kubernetes Sealed Secrets (`kubeseal`) — encrypted secrets safe to commit
   - **Advanced:** External Secrets Operator + cloud secrets manager (AWS SSM, Azure Key Vault)
4. Update CI pipeline to inject secrets from GitHub Secrets (not from repo)
5. Add Gitleaks to CI to prevent future commits of secrets

**Effort:** 2–4 hours

---

### 🔴 1.2 Add SAST and Secret Scanning to CI

**Problem:** No static analysis beyond Aegis governance checks.

**Steps:**
1. Add `security.yml` workflow (see CI-CD-Architecture.md)
2. Enable GitHub CodeQL for C# (free on public repos, part of GitHub Advanced Security)
3. Add Gitleaks pre-commit hook locally + Gitleaks in CI
4. Enable GitHub's built-in secret scanning (Settings → Code security → Secret scanning)

**Effort:** 2–3 hours

---

### 🟠 1.3 Add Environment Protection Rules to Deploy

**Problem:** The `deploy` job runs automatically on every push to `main` with no manual approval gate.

**Steps:**
1. In GitHub → Settings → Environments → `dev-local`: add required reviewers (yourself)
2. Add `wait-timer: 5` (5 minute delay) as a seatbelt
3. Consider splitting `dev-local` (auto-deploy) from `staging` (manual gate) as the system matures

**Effort:** 30 minutes

---

## Phase 2 — Pipeline Modularization (1–2 Sprints)

### 🟠 2.1 Split Monolithic `ci.yml` into 5 Workflows

**Problem:** 500+ line single workflow mixes concerns. Hard to maintain, debug, or re-run individual stages.

**Steps:**
1. Create `.github/workflows/ci.yml` (build + unit tests + integration tests)
2. Create `.github/workflows/governance.yml` (Aegis + dependency audit)
3. Create `.github/workflows/docker.yml` (build + push, triggered by CI success)
4. Create `.github/workflows/security.yml` (Trivy + SAST + Gitleaks)
5. Create `.github/workflows/deploy.yml` (manifest update + rollout + smoke tests)
6. Delete old `ci.yml`

See `CI-CD-Architecture.md` for full workflow designs.

**Effort:** 4–6 hours

---

### 🟠 2.2 Fix Docker Layer Cache Strategy

**Problem:** Current pipeline uses `type=local` cache stored in runner filesystem. The self-hosted runner retains this between runs, but cloud runners (`ubuntu-latest`) do not — wasting cache on non-deploy jobs.

**Steps:**
1. Switch Docker Buildx cache to `type=gha` (GitHub Actions Cache)
2. Scope cache keys per service: `scope=${{ matrix.service }}`
3. Remove manual `rm -rf` + `mv` cache workaround

```yaml
cache-from: type=gha,scope=${{ matrix.service }}
cache-to:   type=gha,mode=max,scope=${{ matrix.service }}
```

**Effort:** 1 hour

---

### 🟡 2.3 Scope Integration Tests Per Service

**Problem:** Integration tests currently only cover `TenantIsolation.Tests`. Other services have integration test files (e.g., `AttendanceIntegrationTests.cs`, `FeeIntegrationTests.cs`) but they may not be wired into CI.

**Steps:**
1. Audit each `*IntegrationTests.cs` file — confirm which ones need a running database
2. Add each integration test project to the `integration-tests` CI matrix
3. Ensure each uses `PostgresContainerFixture` (Testcontainers) for isolation — not a shared runner-level DB
4. Consider Testcontainers over `services: postgres:` for better isolation and per-run DB creation

**Effort:** 3–5 hours

---

## Phase 3 — SharedKernel Deduplication (1 Sprint)

### 🟡 3.1 Extract `OutboxRelayServiceBase` to SharedKernel

**Problem:** `OutboxRelayService` is duplicated verbatim (or near-verbatim) in 9 services: Academic, Attendance, Examination, Faculty, Fee, Hostel, Identity (via outbox pattern), Notification, Student.

**Steps:**
1. Create `UMS.SharedKernel/Infrastructure/OutboxRelayServiceBase.cs`
2. Make it generic over `TDbContext : DbContext`
3. Override `GetUnprocessedMessages()` and `MarkAsProcessed()` via virtual methods
4. Update each service to extend the base class
5. Run full test suite to verify

**Effort:** 3–4 hours

---

### 🟡 3.2 Extract `DomainEventDispatcherInterceptor` to SharedKernel

**Problem:** `DomainEventDispatcherInterceptor` is duplicated in Academic, Attendance, Examination, Faculty, Fee, Hostel, Identity, Student (8 copies).

**Steps:**
1. Create `UMS.SharedKernel/Infrastructure/DomainEventDispatcherInterceptor.cs`
2. Each service's `DependencyInjection.cs` registers it against their own `DbContext`
3. Delete per-service copies

**Effort:** 1–2 hours

---

### 🟡 3.3 Extract `KafkaConsumerBase` to SharedKernel

**Problem:** `KafkaConsumerBase` currently lives in `Notification.Infrastructure` but should be shared. Other services that later add Kafka consumers would need to re-implement it.

**Steps:**
1. Move `KafkaConsumerBase.cs` and `KafkaEventEnvelope.cs` to `UMS.SharedKernel/Kafka/`
2. Update Notification.Infrastructure reference
3. Add `KafkaTopics.cs` to SharedKernel (currently scattered)

**Effort:** 1–2 hours

---

### 🟡 3.4 Extract `GlobalExceptionMiddleware` and `MigrationHostedService` to ServiceDefaults

**Problem:** Both are duplicated across all services.

**Steps:**
1. Move `GlobalExceptionMiddleware.cs` to `ServiceDefaults/`
2. Move `MigrationHostedService.cs` to `ServiceDefaults/` as generic `MigrationHostedService<TContext>`
3. Delete per-service copies; update `Program.cs` references

**Effort:** 2–3 hours

---

### 🟡 3.5 Fix `ValidationBehavior` / `ValidationBehaviour` Naming

**Problem:** The pipeline behaviour is called `ValidationBehavior` in most services but `ValidationBehaviour` (British spelling) in the Student service. Inconsistent naming causes confusion.

**Steps:**
1. Standardize on `ValidationBehavior` (American spelling, matches MediatR convention)
2. Move to `UMS.SharedKernel/Application/ValidationBehavior.cs` (once, not 9 copies)
3. Each service registers it in their DI extension

**Effort:** 1 hour

---

## Phase 4 — Testing Improvements (Ongoing)

### 🟡 4.1 Add Integration Test for Each Service

**Current state:** Per-service integration tests exist in the service's `.Tests` project but appear lightweight. Dedicated cross-cutting tests are limited to `TenantIsolation.Tests`.

**Recommended test project structure:**

```
src/Tests/
├── TenantIsolation.Tests/      ← cross-tenant data isolation (exists)
├── Identity.IntegrationTests/  ← OpenIddict token flow (exists: OpenIddictTokenFlowTests)
├── Kafka.IntegrationTests/     ← event publish/consume round-trip (exists)
├── AppHost.IntegrationTests/   ← full Aspire stack smoke (exists)
└── Contract.Tests/             ← NEW: consumer-driven contract tests (Pact)
```

**Steps:**
1. Ensure each existing integration test project is wired into CI matrix
2. Add Testcontainers to replace runner-level `services: postgres:` for better isolation
3. Future: add Pact contract testing for Kafka event schemas

**Effort:** 4–8 hours

---

### 🟡 4.2 Enforce Coverage Thresholds

**Problem:** Coverage reports are generated but no threshold enforcement exists. Tests could be deleted without CI failing.

**Steps:**
1. Add to each test project's `*.csproj`:
   ```xml
   <CoverageThreshold>
     <Line>70</Line>
     <Branch>60</Branch>
   </CoverageThreshold>
   ```
2. Add `--coverage-minimum-threshold` flag to `dotnet test` in CI

**Effort:** 1 hour

---

## Phase 5 — Observability Improvements (1 Sprint)

### 🟢 5.1 Enable Prometheus Metrics-Server in Minikube

**Current state:** Prometheus + Grafana are deployed but `metrics-server` is not enabled in Minikube (noted in README pending list).

**Steps:**
1. `minikube addons enable metrics-server`
2. Configure Prometheus to scrape all service `/metrics` endpoints
3. Import or build Grafana dashboards for per-service throughput, latency, error rates

**Effort:** 2–4 hours

---

### 🟢 5.2 Add Correlation ID Middleware

**Problem:** Distributed tracing via Jaeger is configured but correlation IDs may not propagate correctly from API Gateway through all service hops.

**Steps:**
1. Add `CorrelationIdMiddleware` to `ServiceDefaults`
2. API Gateway forwards `X-Correlation-Id` header
3. All services enrich Serilog log context with correlation ID
4. Verify in Jaeger that traces span multiple services end-to-end

**Effort:** 2–3 hours

---

## Phase 6 — Repository Structure Improvements (Polish)

### 🟢 6.1 Rename Solution File

**Current:** `AspireApp1.slnx` (scaffolded name)
**Recommended:** `UMS.slnx`

**Steps:**
1. Rename file
2. Update all CI references (`dotnet restore UMS.slnx`)
3. Update README

**Effort:** 15 minutes

---

### 🟢 6.2 Add `Architecture Decision Records (ADRs)`

**Problem:** There are no documented reasons for key decisions (why Kafka over RabbitMQ, why YARP, why OpenIddict, why outbox pattern).

**Steps:**
1. Create `docs/adr/` directory
2. Write ADRs for: Kafka, YARP, OpenIddict, Outbox Pattern, Aegis Governance, Kustomize
3. Link from README

**Effort:** 2–4 hours

---

## Summary Roadmap

```
Phase 1 (Week 1):   Security + Deploy protection
Phase 2 (Week 2):   Split CI/CD pipeline
Phase 3 (Week 3-4): SharedKernel deduplication
Phase 4 (Ongoing):  Testing improvements
Phase 5 (Sprint 3): Observability
Phase 6 (Polish):   Structure + docs
```

| Phase | Items | Total Effort |
|-------|-------|-------------|
| 1 — Security | 1.1, 1.2, 1.3 | ~5 hours |
| 2 — Pipeline | 2.1, 2.2, 2.3 | ~10 hours |
| 3 — SharedKernel | 3.1–3.5 | ~10 hours |
| 4 — Testing | 4.1, 4.2 | ~9 hours |
| 5 — Observability | 5.1, 5.2 | ~6 hours |
| 6 — Polish | 6.1, 6.2 | ~5 hours |
| **Total** | | **~45 hours** |
