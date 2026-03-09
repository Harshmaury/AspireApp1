# CI/CD Architecture
## University Management System (UMS)

---

## 1. Current Pipeline Assessment

### Structure

The current `ci.yml` is a single monolithic workflow file with 8 jobs:

```
detect-changes → build → governance ─┐
                                     ├→ unit-tests (9 parallel)
                                               └→ integration-tests
                                                        └→ test-summary
                                                                  └→ docker (11 parallel)
                                                                            └→ update-manifests
                                                                                     └→ deploy
```

### Strengths of Current Pipeline

| Strength | Detail |
|----------|--------|
| Change detection | `dorny/paths-filter` accurately scopes work per service |
| Parallel unit tests | 9 services run simultaneously, `fail-fast: false` |
| Governance gate | Aegis blocks Docker push if architectural rules fail |
| Artifact reuse | Build output uploaded once, test results collected centrally |
| Trivy scanning | Container vulnerability scanning → SARIF → GitHub Security tab |
| Kustomize-based deploy | Clean YAML-only K8s manifest management |
| Snapshot baseline | Architecture baseline auto-updated post-deploy |
| Concurrency control | `cancel-in-progress: true` prevents queue buildup |

### Problems with Current Pipeline

| Problem | Impact | Severity |
|---------|--------|----------|
| Single 500+ line workflow | Hard to maintain, debug, own separately | High |
| Security scanning inside `docker` job | Can't run independently; blocked by full build chain | High |
| Governance mixed with build | Can't rerun governance check independently | Medium |
| Deploy job uses `self-hosted` runner | If runner is offline, entire pipeline stalls | High |
| No workflow-level separation by concern | A test failure blocks Docker; Docker failure blocks deploy | Medium |
| Integration tests only test `TenantIsolation` | Limited coverage scope for integration stage | Medium |
| Docker job waits on `integration-tests` AND `governance` | Longest possible critical path | Medium |
| `update-manifests` and `deploy` in same repo | GitOps not fully separated; commit noise on main | Low |
| No SAST/secret scanning | No static analysis beyond Aegis governance | High |
| No environment protection rules | Deploy to dev-local has no approval gate | Medium |
| `secret.yaml` in-repo | Secrets committed (K8s secret but still visible) | Critical |

---

## 2. Recommended Pipeline Architecture

### Principle: One Workflow Per Concern

```
.github/workflows/
├── ci.yml           ← change detection + build + unit tests
├── governance.yml   ← architecture rules + event contracts + snapshot drift
├── docker.yml       ← container build + push to GHCR
├── security.yml     ← Trivy scan + SAST + secret scanning
└── deploy.yml       ← K8s manifest update + rollout + smoke tests
```

### Workflow Trigger Matrix

| Workflow | Push (main) | Push (feature/*) | PR | Schedule |
|----------|-------------|------------------|----|----------|
| ci.yml | ✓ | ✓ | ✓ | — |
| governance.yml | ✓ | if changed | ✓ | weekly |
| docker.yml | ✓ | — | — | — |
| security.yml | ✓ | — | ✓ | daily |
| deploy.yml | ✓ | — | — | — |

---

## 3. New Workflow Designs

---

### 3.1 `ci.yml` — Build, Unit Tests, Integration Tests

```yaml
name: CI

on:
  push:
    branches: [main, 'feature/**']
  pull_request:
    branches: [main]

concurrency:
  group: ci-${{ github.ref }}
  cancel-in-progress: true

jobs:
  detect-changes:
    # ... (identical to current — paths-filter per service)

  build:
    needs: detect-changes
    # dotnet restore + build → upload build-output artifact

  unit-tests:
    needs: [build, detect-changes]
    strategy:
      matrix: [9 services, change-aware]
    # parallel per-service test + coverage

  integration-tests:
    needs: unit-tests
    if: github.ref == 'refs/heads/main' || github.event_name == 'pull_request'
    services: [postgres]
    # TenantIsolation.Tests + any new integration test projects

  test-summary:
    needs: [unit-tests, integration-tests]
    if: always()
    # TRX aggregation + ReportGenerator coverage
```

**Artifacts produced:**
- `build-output` (1 day retention)
- `unit-test-results-{service}` (7 days)
- `coverage-{service}` (7 days)
- `integration-test-results` (7 days)
- `coverage-merged` (7 days)

---

### 3.2 `governance.yml` — Architecture Rules

```yaml
name: Governance

on:
  push:
    branches: [main]
    paths:
      - 'src/**/*.cs'
      - 'src/Governance/**'
      - 'src/Cli/**'
      - 'aegis.config.json'
  pull_request:
    branches: [main]
  schedule:
    - cron: '0 2 * * 1'   # weekly Monday 02:00 UTC

jobs:
  aegis:
    name: Aegis Governance Gate
    runs-on: ubuntu-latest
    steps:
      # Restore + build CLI
      # govern verify all → governance-report.json
      # govern snapshot diff baseline-latest → fail on drift
      # verify-event-contracts → event-contracts-report.json
      # PR annotation with violations
      # Upload reports (7 days)

  # NEW: dependency validation
  dependency-check:
    name: Validate NuGet Dependencies
    runs-on: ubuntu-latest
    steps:
      - run: dotnet list package --vulnerable --include-transitive
      - run: dotnet list package --deprecated
```

**Key change:** Governance is now a standalone workflow. It can be re-run independently, scheduled, and its status is reported separately from CI.

---

### 3.3 `docker.yml` — Container Build + Push

```yaml
name: Docker Build

on:
  workflow_run:
    workflows: ["CI"]
    types: [completed]
    branches: [main]

jobs:
  gate:
    name: Check CI Status
    runs-on: ubuntu-latest
    outputs:
      should_run: ${{ steps.check.outputs.result }}
    steps:
      - id: check
        run: |
          echo "result=${{ github.event.workflow_run.conclusion == 'success' }}" >> $GITHUB_OUTPUT

  build-push:
    name: "Docker: ${{ matrix.service }}"
    needs: gate
    if: needs.gate.outputs.should_run == 'true'
    strategy:
      matrix: [11 services, change-aware via workflow_run artifacts]
    permissions:
      contents: read
      packages: write
    steps:
      # docker/login-action (GHCR)
      # docker/setup-buildx-action
      # docker/build-push-action
      #   cache-from: type=gha,scope=${{ matrix.service }}
      #   cache-to:   type=gha,mode=max,scope=${{ matrix.service }}
      # Upload image digest as artifact
```

**Key changes:**
- Triggered by successful CI completion (not inline)
- Uses GitHub Actions cache (`type=gha`) instead of local filesystem cache
- Security scanning moved to `security.yml`
- No `exit-code: 1` on Trivy here — security is a separate gate

---

### 3.4 `security.yml` — Scanning

```yaml
name: Security

on:
  workflow_run:
    workflows: ["Docker Build"]
    types: [completed]
    branches: [main]
  pull_request:
    branches: [main]
  schedule:
    - cron: '0 3 * * *'   # daily 03:00 UTC

permissions:
  security-events: write
  contents: read

jobs:
  trivy-containers:
    name: "Trivy: ${{ matrix.service }}"
    strategy:
      matrix: [11 services]
    steps:
      # Pull image from GHCR (digest from docker.yml artifact)
      # aquasecurity/trivy-action → SARIF → upload to Security tab
      # CRITICAL vulnerabilities set exit-code: 1 (block deploy)

  trivy-filesystem:
    name: Trivy Filesystem Scan
    steps:
      # Scan src/ for misconfigurations and secret patterns
      # aquasecurity/trivy-action --scan-type fs

  secret-scanning:
    name: Secret Scanning (Gitleaks)
    steps:
      # gitleaks/gitleaks-action
      # Scans for committed secrets, API keys, connection strings

  sast:
    name: SAST (CodeQL)
    steps:
      # github/codeql-action/init (csharp)
      # dotnet build
      # github/codeql-action/analyze
      # Results → Security tab
```

**Key change:** Security scanning is now a daily scheduled job AND runs on PRs, completely independent of the build/deploy chain. Failures create security alerts but don't block deploys (except CRITICAL Trivy findings).

---

### 3.5 `deploy.yml` — Kubernetes Deployment

```yaml
name: Deploy

on:
  workflow_run:
    workflows: ["Docker Build"]
    types: [completed]
    branches: [main]

jobs:
  update-manifests:
    name: Update K8s Image Tags
    runs-on: ubuntu-latest
    if: ${{ github.event.workflow_run.conclusion == 'success' }}
    permissions:
      contents: write
    steps:
      # Install yq
      # Retrieve image tags from docker.yml artifacts
      # yq update all deployment manifests
      # git commit k8s/ [skip ci]

  deploy:
    name: Deploy to Minikube
    runs-on: self-hosted
    needs: update-manifests
    environment:
      name: dev-local
      url: http://localhost:8080
    steps:
      # git pull
      # minikube image load (only changed services)
      # kubectl apply -k k8s/overlays/dev-local
      # kubectl rollout status
      # Smoke tests (all 10 health endpoints)
      # Update architecture baseline snapshot
      # Deployment summary

  notify-failure:
    name: Notify on Failure
    needs: [update-manifests, deploy]
    if: failure()
    steps:
      # Could post to Slack/Teams/email
      # Or create a GitHub issue
      - run: echo "Deploy failed for ${{ github.sha }}"
```

---

## 4. Critical Path Comparison

### Before (single `ci.yml`)

```
detect (30s)
  → build (3-5 min)
    → governance (2-3 min)
      → unit-tests (2-4 min parallel)
        → integration-tests (2-3 min)
          → test-summary (1 min)
            → docker (5-8 min per service, 11 parallel)
              → update-manifests (1 min)
                → deploy (3-5 min)

TOTAL CRITICAL PATH: ~22-35 min
All jobs in one chain — any failure blocks everything downstream
```

### After (5 separate workflows)

```
CI workflow:
  detect (30s) → build (3-5 min) → unit-tests (2-4 min) → integration-tests (2-3 min)
  TOTAL: ~8-13 min (provides fast feedback on PRs)

Docker workflow (triggered by CI):
  build-push (5-8 min, 11 parallel)
  TOTAL: ~5-8 min

Security workflow (concurrent with Docker):
  trivy + sast + secrets (3-5 min)
  TOTAL: ~3-5 min

Deploy workflow (triggered by Docker):
  update-manifests (1 min) → deploy (3-5 min)
  TOTAL: ~4-6 min

Governance workflow (parallel, own trigger):
  aegis (2-3 min)

TOTAL END-TO-END: ~20-30 min (similar), BUT:
  - PRs only run CI (~8-13 min) — much faster feedback
  - Governance failures don't block Docker build
  - Security issues visible independently
  - Deploy can be re-run without re-running all tests
```

---

## 5. Artifact Passing Strategy

```
ci.yml          → uploads: build-output, test-results-*, coverage-*
docker.yml      → downloads: build-output (for cache key)
                → uploads: image-digests.json (per-service SHA256 digest)
security.yml    → downloads: image-digests.json (to scan specific digest)
deploy.yml      → downloads: image-digests.json (to update manifests)
governance.yml  → standalone (reads source only)
```

---

## 6. Environment Strategy

| Environment | Runner | Protection | Used by |
|-------------|--------|------------|---------|
| `ci` | `ubuntu-latest` | None | ci.yml |
| `docker` | `ubuntu-latest` | None | docker.yml |
| `dev-local` | `self-hosted` | Manual approval (recommended) | deploy.yml |
| `staging` | `self-hosted` | Auto-deploy on green | future |
| `production` | `self-hosted` | Required reviewer | future |

---

## 7. Self-Hosted Runner Resilience

Current risk: the self-hosted WSL2 runner is a single point of failure for Deploy.

**Recommendations:**

1. Add a runner health check step that retries with 3-minute timeout before failing
2. Add `notify-failure` job that creates a GitHub Issue on deploy failure
3. Document runner restart procedure in workflow comments
4. Consider `actions-runner-controller` on Minikube for runner self-healing

---

## 8. Security Improvements

| Issue | Recommendation |
|-------|---------------|
| `secret.yaml` in repo | Move to external secret manager (Vault, AWS Secrets Manager, Sealed Secrets) |
| No SAST | Add CodeQL to `security.yml` (C# support) |
| No secret scanning | Add Gitleaks to `security.yml` |
| Trivy `exit-code: 0` | Separate blocking (CRITICAL) from advisory (HIGH) findings |
| GITHUB_TOKEN used for GHCR push | Fine for personal repo; use dedicated token for org repos |
| No dependency audit | Add `dotnet list package --vulnerable` to `governance.yml` |
