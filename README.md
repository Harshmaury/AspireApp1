# UMS — University Management System
## Complete Project Reference Guide
> Last Updated: 2026-03-05 | .NET 10 | Aspire | Kubernetes | Minikube

---

## 1. Project Locations

| Where | Path |
|---|---|
| Windows Source | `C:\Users\harsh\source\repos\AspireApp1` |
| WSL2 Mount | `/mnt/c/Users/harsh/source/repos/AspireApp1` |
| GitHub | `https://github.com/Harshmaury/AspireApp1` |
| GitHub Actions | `https://github.com/Harshmaury/AspireApp1/actions` |
| Container Registry | `ghcr.io/harshmaury/ums/<service>` |

---

## 2. System Architecture

```
┌─────────────────────────────────────────────┐
│           GitHub Actions CI/CD              │
│   Build → Test → Docker → Deploy           │
│       (self-hosted runner on WSL2)          │
└──────────────────┬──────────────────────────┘
                   │ git push to main
┌──────────────────▼──────────────────────────┐
│              Minikube (WSL2)                │
│                                             │
│  ┌─────────┐        ┌─────────┐            │
│  │ Ingress │        │   BFF   │            │
│  └────┬────┘        └────┬────┘            │
│       └────────┬─────────┘                 │
│         ┌──────▼──────┐                    │
│         │ API Gateway │  (YARP)            │
│         └──┬──┬──┬───┘                     │
│    ┌───────┘  │  └───────┐                 │
│  9 Microservices (Clean Architecture)       │
│    │                     │                 │
│  ┌─▼────────┐    ┌───────▼──────┐          │
│  │ Kafka    │    │  PostgreSQL  │          │
│  │ 9 topics │    │  per-svc DB  │          │
│  └──────────┘    └──────────────┘          │
│  ┌──────────┐    ┌──────────────┐          │
│  │   Seq    │    │    Jaeger    │          │
│  │  (logs)  │    │  (traces)    │          │
│  └──────────┘    └──────────────┘          │
└─────────────────────────────────────────────┘
```

---

## 3. Technology Stack

| Layer | Technology |
|---|---|
| Local Dev | .NET Aspire 10 (Windows) |
| Language | C# 13 / .NET 10 |
| Container Orchestration | Minikube (Docker driver, WSL2) |
| API Gateway | YARP reverse proxy |
| BFF | ASP.NET Core minimal API |
| Auth | OpenIddict (password flow + refresh tokens) |
| Messaging | Kafka (9 topics, 3 partitions each) |
| Database | PostgreSQL 16 (per-service DB, EF Core migrations) |
| Logging | Seq (structured logs via Serilog) |
| Tracing | Jaeger (OpenTelemetry distributed tracing) |
| CI/CD | GitHub Actions → self-hosted runner on WSL2 |
| Pattern | Clean Architecture + CQRS + MediatR |
| Migrations | MigrationHostedService (non-blocking, runs on startup) |

---

## 4. Solution Structure

```
AspireApp1/
├── src/
│   ├── AppHost/                  ← .NET Aspire orchestration
│   ├── ServiceDefaults/          ← Shared service config (health, OTEL, Serilog)
│   ├── ApiGateway/               ← YARP reverse proxy
│   ├── BFF/                      ← Backend for Frontend
│   ├── Shared/                   ← SharedKernel (base entities, extensions)
│   ├── Governance/               ← Policy / compliance layer
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
├── k8s/
│   ├── base/                     ← Base K8s manifests
│   │   ├── services/             ← Per-service deployments + services
│   │   ├── gateway/              ← API Gateway deployment
│   │   ├── bff/                  ← BFF deployment
│   │   ├── infra/                ← Kafka, Postgres, Zookeeper, Seq, Jaeger
│   │   ├── configmap-base.yaml
│   │   └── secret.yaml           ← All secrets (connection strings, OpenIddict keys)
│   └── overlays/
│       └── dev-local/            ← Local dev overrides (kustomization.yaml)
└── .github/
    └── workflows/
        └── ci.yml                ← 7-stage CI/CD pipeline
```

---

## 5. Each Microservice Structure (Clean Architecture)

```
Services/Identity/
├── Identity.API/              ← Endpoints, Middleware, Dockerfile, Program.cs
├── Identity.Application/      ← Commands, Queries, Handlers (CQRS + MediatR)
├── Identity.Domain/           ← Entities, Domain Events, Exceptions
├── Identity.Infrastructure/   ← DbContext, Repos, Kafka, EF Migrations
└── Identity.Tests/            ← Unit tests (xUnit)
```

---

## 6. All 9 Microservices

| Service | DbContext | Kafka Topic | K8s Port |
|---|---|---|---|
| Identity | ApplicationDbContext | identity-events | 5002 |
| Academic | AcademicDbContext | academic-events | 5004 |
| Student | StudentDbContext | student-events | 5003 |
| Attendance | AttendanceDbContext | attendance-events | 5005 |
| Examination | ExaminationDbContext | examination-events | 5006 |
| Fee | FeeDbContext | fee-events | 5007 |
| Faculty | FacultyDbContext | faculty-events | 5008 |
| Hostel | HostelDbContext | hostel-events | 5009 |
| Notification | NotificationDbContext | notification-events | 5010 |

---

## 7. Access URLs (via port-forward)

| Service | URL | Notes |
|---|---|---|
| API Gateway | http://localhost:8080 | Main entry point |
| API Gateway Health | http://localhost:8080/health | Should return "Healthy" |
| BFF | http://localhost:5001 | Backend for frontend |
| Identity API | http://localhost:5002 | Auth + token endpoint |
| Seq Logs | http://localhost:8081 | Structured log viewer |
| Jaeger Traces | http://localhost:16686 | Distributed tracing UI |

---

## 8. Auth Flow (OpenIddict)

- Flow: **Resource Owner Password + Refresh Token**
- Token endpoint: `POST http://localhost:5002/connect/token`
- Client ID: `api-gateway`
- Client Secret: stored in `ums-secrets` → `OpenIddict__ClientSecret`
- Signing Key: RSA 2048-bit XML → `OpenIddict__SigningKeyXml`
- Encryption Key: RSA 2048-bit XML → `OpenIddict__EncryptionKeyXml`
- Dev mode uses ephemeral keys (no secrets needed)
- Production (Minikube) uses persistent RSA keys from Kubernetes secret

**Get a token:**
```bash
curl -X POST http://localhost:5002/connect/token \
  -d "grant_type=password" \
  -d "username=admin@ums.local" \
  -d "password=Admin123!" \
  -d "client_id=api-gateway" \
  -d "client_secret=api-gateway-secret-dev" \
  -d "scope=api openid offline_access"
```

---

## 9. Kubernetes Secrets Reference

All secrets live in `k8s/base/secret.yaml` and are applied as `ums-secrets`.

| Key | Purpose |
|---|---|
| `ConnectionStrings__<Service>Db` | Primary DB connection per service |
| `ConnectionStrings__<Service>DbReadOnly` | Read replica connection |
| `POSTGRES_USER` | PostgreSQL username |
| `POSTGRES_PASSWORD` | PostgreSQL password |
| `OpenIddict__ClientSecret` | API Gateway client secret |
| `OpenIddict__SigningKeyXml` | RSA key for signing JWT tokens |
| `OpenIddict__EncryptionKeyXml` | RSA key for encrypting JWT tokens |

**If you ever need to regenerate RSA keys:**
```bash
openssl genrsa -out /tmp/signing.pem 2048
python3 << 'EOF'
import base64
from cryptography.hazmat.primitives.serialization import load_pem_private_key
from cryptography.hazmat.backends import default_backend

with open('/tmp/signing.pem', 'rb') as f:
    key = load_pem_private_key(f.read(), password=None, backend=default_backend())
pub = key.public_key().public_numbers()
priv = key.private_numbers()
def to_b64(n):
    return base64.b64encode(n.to_bytes((n.bit_length()+7)//8,'big')).decode()
xml = f"<RSAKeyValue><Modulus>{to_b64(pub.n)}</Modulus><Exponent>{to_b64(pub.e)}</Exponent><P>{to_b64(priv.p)}</P><Q>{to_b64(priv.q)}</Q><DP>{to_b64(priv.dmp1)}</DP><DQ>{to_b64(priv.dmq1)}</DQ><InverseQ>{to_b64(priv.iqmp)}</InverseQ><D>{to_b64(priv.d)}</D></RSAKeyValue>"
print(base64.b64encode(xml.encode()).decode())
EOF
```

---

## 10. CI/CD Pipeline (7 Stages)

```
git push to main / feature/*
          │
          ▼
┌─────────────────┐
│  1. Build       │  dotnet build AspireApp1.slnx --configuration Release
└────────┬────────┘
         ▼
┌─────────────────┐
│  2. Unit Tests  │  All 9 services in parallel matrix (xUnit + TRX reports)
└────────┬────────┘
         ▼
┌─────────────────┐
│  3. Integration │  TenantIsolation.Tests (spins up real Postgres via service)
│     Tests       │
└────────┬────────┘
         ▼
┌─────────────────┐
│  4. Test Summary│  dorny/test-reporter → GitHub PR summary
└────────┬────────┘  (main branch only below)
         ▼
┌─────────────────┐
│  5. Docker      │  Build + push all 11 images to ghcr.io in parallel
│     Build       │  Tagged: <sha8> + latest
└────────┬────────┘
         ▼
┌─────────────────┐
│  6. Update      │  sed image tags in k8s YAML files → auto-commit [skip ci]
│    Manifests    │
└────────┬────────┘
         ▼
┌─────────────────┐
│  7. Deploy      │  runs-on: self-hosted (your WSL2 runner)
│   (Minikube)    │  kubectl apply -k k8s/overlays/dev-local
└─────────────────┘
```

**Triggers:**
- Push to `main` → full 7-stage pipeline
- Push to `feature/**` → stages 1–4 only (no Docker, no deploy)
- Pull request to `main` → stages 1–4

---

## 11. GitHub Actions Runner

| Property | Value |
|---|---|
| Location | `~/actions-runner/` |
| Name | `wsl2-minikube` |
| Labels | `self-hosted, linux, minikube` |
| Service | `actions.runner.Harshmaury-AspireApp1.wsl2-minikube` |

```bash
# Check status
sudo systemctl status actions.runner.Harshmaury-AspireApp1.wsl2-minikube

# Restart
sudo systemctl restart actions.runner.Harshmaury-AspireApp1.wsl2-minikube

# View live logs
journalctl -u actions.runner.Harshmaury-AspireApp1.wsl2-minikube -f
```

---

## 12. dev CLI — All Commands

The `dev` command lives at `~/dev` and is available globally in WSL2.

```bash
dev start               # Start Minikube + pods + Kafka topics + port-forwards + runner
dev stop                # Kill port-forwards (Minikube keeps running)
dev stop --hard         # Kill port-forwards + stop Minikube
dev restart             # stop then start
dev status              # Live dashboard: pods, ports, Kafka, CPU/MEM
dev deploy <svc>        # Build Docker image → load into Minikube → restart pod
dev logs                # List all pods
dev logs <svc>          # Stream logs for a service (Ctrl+C to stop)
dev watch               # Auto-deploy on file save (uses inotifywait)
dev open seq            # Open Seq log viewer URL
dev open jaeger         # Open Jaeger tracing UI URL
dev open health         # curl API Gateway health endpoint
dev open bff            # Open BFF URL
dev recovery            # Auto-detect and fix: crashed pods, missing topics, dropped port-forwards
dev git "message"       # git add . && commit && push in one command
dev help                # Show all commands
```

**Service names for `dev deploy` and `dev logs`:**
```
identity-api    academic-api    student-api     attendance-api
examination-api fee-api         faculty-api     hostel-api
notification-api  api-gateway   bff
```

---

## 13. Coding with Visual Studio 2022/2026 (Windows)

### Opening the Project
1. Open Visual Studio
2. File → Open → Project/Solution
3. Select `C:\Users\harsh\source\repos\AspireApp1\AspireApp1.slnx`

### Running Locally (Aspire — Fastest for Development)
```powershell
# In PowerShell — kill any stale processes first
Get-Process -Name "dotnet","BFF","dcp","dcpctrl","dcpproc" -ErrorAction SilentlyContinue | Stop-Process -Force

# Start Aspire (runs everything locally, no Docker needed)
dotnet run --project src\AppHost\AspireApp1.AppHost.csproj --launch-profile https
```
- Aspire Dashboard opens automatically in browser
- Hot-reload works — save a file and it reloads instantly
- No Minikube, no Docker needed for this mode
- Use this for rapid feature development

### Adding a New Feature (Example: add endpoint to Identity)
1. Open `src/Services/Identity/` in Solution Explorer
2. Add Command in `Identity.Application/Commands/`
3. Add Handler in `Identity.Application/Handlers/`
4. Add Endpoint in `Identity.API/Endpoints/`
5. Save → Aspire hot-reloads OR run `dev deploy identity-api` in WSL2

### Adding a New Service
1. Copy an existing service folder structure
2. Register it in `src/AppHost/AppHost.cs`
3. Add to `~/ums/config/services.conf`
4. Add port to `~/ums/config/ports.conf` if needed
5. Add path to `SERVICE_MAP` in `~/ums/engine/ums-watch.sh`
6. Add K8s deployment + service YAML in `k8s/base/services/`
7. Add to `kustomization.yaml`

### Running Tests in Visual Studio
- Test Explorer → Run All
- Or per service: right-click test project → Run Tests
- Integration tests need Postgres running (use Aspire or Docker)

### Debugging in Visual Studio
- Set breakpoint in any service
- Run via Aspire (F5 from AppHost project)
- Breakpoints work across all services simultaneously

---

## 14. Daily Dev Workflow

### Every Morning (WSL2)
```bash
# Terminal 1
dev start

# Terminal 2 (keep open while coding)
dev watch
```

### Making a Code Change
**Option A — Automatic (recommended):**
Save any `.cs` file → watcher detects service → rebuilds → redeploys in ~30s

**Option B — Manual deploy:**
```bash
dev deploy identity-api
```

**Option C — Full CI/CD via GitHub:**
```bash
dev git "feat: add profile update endpoint"
```
GitHub Actions: Build → Test → Docker → Deploy automatically

### Checking What's Wrong
```bash
dev status                          # full dashboard
dev logs identity-api               # stream logs
kubectl describe pod -n ums <pod>   # pod events
dev recovery                        # auto-fix everything
```

---

## 15. Engine Scripts Location

| Script | Purpose |
|---|---|
| `~/dev` | CLI entry point |
| `~/ums/engine/ums-engine.sh` | main orchestrator |
| `~/ums/engine/ums-deploy.sh` | Per-service build + deploy |
| `~/ums/engine/ums-watch.sh` | File watcher + auto-deploy |
| `~/ums/engine/ums-status.sh` | Status dashboard |
| `~/ums/engine/ums-recovery.sh` | Auto error recovery |
| `~/ums/engine/ums-health.sh` | Background health monitor |
| `~/ums/config/services.conf` | Service registry |
| `~/ums/config/ports.conf` | Port mapping |
| `~/ums/logs/` | All engine + port-forward logs |
| `~/ums/signing_key.backup.txt` | RSA signing key backup |
| `~/ums/encryption_key.backup.txt` | RSA encryption key backup |

---

## 16. Troubleshooting

| Symptom | Fix |
|---|---|
| `dev` not found | `source ~/.bashrc` |
| Port-forwards dropped | `dev start` |
| Pod in CrashLoopBackOff | `dev logs <svc>` then `dev recovery` |
| Kafka topics missing | `dev recovery` |
| identity-api crashing | Check `OpenIddict__SigningKeyXml` in `ums-secrets` |
| Build fails in CI | Check https://github.com/Harshmaury/AspireApp1/actions |
| Runner offline | `sudo systemctl restart actions.runner.Harshmaury-AspireApp1.wsl2-minikube` |
| Minikube won't start | `minikube delete && minikube start --driver=docker --cpus=4 --memory=8192` |
| After minikube delete | `kubectl apply -k k8s/overlays/dev-local` then `dev start` |
| inotifywait missing | `sudo apt-get install -y inotify-tools` |
| Docker build fails | Check Dockerfile path in `~/ums/config/services.conf` |
| Watcher not triggering | Check file extension is `.cs/.csproj/.json/.yaml` |

---

## 17. Optional Background Health Monitor

Runs every 60 seconds, auto-recovers issues silently:
```bash
# Start
nohup ~/ums/engine/ums-health.sh >> ~/ums/logs/health-monitor.log 2>&1 &
echo $! > ~/ums/health-monitor.pid

# Stop
kill $(cat ~/ums/health-monitor.pid)

# View logs
tail -f ~/ums/logs/health-monitor.log
```

Add to `~/.bashrc` to auto-start with WSL2:
```bash
if ! pgrep -f ums-health.sh > /dev/null 2>&1; then
  nohup ~/ums/engine/ums-health.sh >> ~/ums/logs/health-monitor.log 2>&1 &
fi
```

---

## 18. Pending / Next Steps

- [ ] Grafana + Prometheus monitoring dashboards
- [ ] End-to-end CI/CD pipeline test (push to main → verify full deploy)
- [ ] metrics-server for `dev status` resource usage: `minikube addons enable metrics-server`
- [ ] Aspire → Minikube image sync streamlining




