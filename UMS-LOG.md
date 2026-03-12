# UMS-LOG
> Read this first. Ask nothing. Start working.

## WHO
- **Dev:** Harsh Maury | **OS:** Windows 11 + WSL2 (Ubuntu 24) | **Shell:** PowerShell (VS 2026)
- **Repo:** `C:\Users\harsh\source\repos\AspireApp1` | **Branch:** `main`
- **Drop Zone:** `C:\Users\harsh\Downloads\ums-drop\<KEY>\`
- **Workflow doc:** `UMS-WORKFLOW.md` (root) — full key list, architecture rules, drop protocol

## HOW WE WORK
```
1. Claude requests files by KEY         → 📥 FILES NEEDED — UMS-XXX-P0-001
2. Harsh zips them (smart name):        → UMS-XXX-P0-001_20260312-1100.zip
3. Harsh uploads + types:               → dropped UMS-XXX-P0-001
4. Claude outputs:                      → ANALYSIS / CHANGES / COPY BACK / RUN / VERIFY / NEXT
5. Harsh applies, verifies, types:      → done UMS-XXX-P0-001
6. Claude updates this log + commits    → log: UMS-XXX-P0-001 done — one line
```

## STACK
| Layer | Tech |
|---|---|
| Runtime | .NET 10 / C# 13 |
| Architecture | Clean Architecture · DDD · CQRS + MediatR · Minimal API |
| Messaging | Kafka · Outbox pattern · `OutboxRelayServiceBase<T>` |
| DB | PostgreSQL 16 · EF Core 10.0.3 · one DB per service |
| Auth | OpenIddict · password grant · JWT |
| Multi-tenancy | `BaseEntity.TenantId` · EF global query filters · `ITenantContext` |
| Shared | `UMS.SharedKernel` — BaseEntity, AggregateRoot, DomainEvent, OutboxMessage, ValidationBehavior, KafkaConsumerBase |
| Infra | Minikube · Kustomize · Docker · GHCR |
| Observe | OpenTelemetry · Serilog → Seq · Jaeger · Prometheus · Grafana |
| Governance | Aegis rule engine · `Ums.Cli` · CI gate on every push |
| CI/CD | 5 GitHub Actions workflows → ci → docker-build → security → deploy |

## DONE ✅
| Key | Summary | Commit |
|---|---|---|
| `UMS-WORKFLOW-v1` | Workflow doc created | `533401f` |
| `UMS-WORKFLOW-v2` | Full tree audit — 27 keys | `5d8042c` |
| `UMS-SHARED-P0-001` | EF Core already in csproj — confirmed no change needed | — |
| `UMS-SVC-P1-001-NTF` | MimeKit already at 4.12.0 in Directory.Packages.props | — |
| `UMS-REPO-P1-004` | Deleted Script/ debug artefacts | — |
| `UMS-REPO-P1-005` | Deleted student_full_dump.txt + student_structure_map.txt | — |
| `UMS-REPO-P1-006` | Deleted AspireApp1.slnx (UMS.slnx is canonical) | — |
| `UMS-INFRA-P1-005` | Deleted empty Controllers/ folders from all 9 APIs | — |
| `UMS-TEST-P2-001` | Deleted UnitTest1.cs from Aegis.Tests | — |
| `UMS-SHARED-P0-002` | Added BaseEntity · IDomainEvent · DomainEvent · AggregateRoot · fixed OutboxRelay TenantId bug | pending push |

## IN PROGRESS 🔄
| Key | Status | Waiting On |
|---|---|---|
| `UMS-SHARED-P0-003` | Files requested — drop pending | `Domain/Common/` from all 8 services + 2 csproj files |

## OPEN BACKLOG
### P0
| Key | Title |
|---|---|
| `UMS-SEC-P0-001` | Purge secret.yaml + _backups + binDebug from git history |
| `UMS-SVC-P0-001-STU` | Move StudentOutboxRelayService → Student.Infrastructure |
| `UMS-SVC-P0-002-FAC` | Move FacultyOutboxRelayService → Faculty.Infrastructure |

### P1
| Key | Title |
|---|---|
| `UMS-REPO-P1-001` | Remove _backups/ from git history |
| `UMS-REPO-P1-002` | Remove binDebug/ from git history |
| `UMS-REPO-P1-003` | Delete all .bak files + .gitignore rules |
| `UMS-INFRA-P1-001` | Remove per-service TenantMiddleware copies (Academic, Attendance, Faculty) |
| `UMS-INFRA-P1-002` | Remove duplicate TenantContext from SharedKernel |
| `UMS-INFRA-P1-003` | Remove duplicate MigrationHostedService from ServiceDefaults root |
| `UMS-INFRA-P1-004` | Delete empty Persistence/Migrations/ folders (Attendance, Faculty) |
| `UMS-INFRA-P1-006` | Remove ApiService + Web Aspire scaffold projects from solution |
| `UMS-GOV-P1-001` | Commit governance baselines + create event-schemas dir |
| `UMS-SVC-P1-002` | KubernetesClient CVE 16.0.7 → 16.1.0 |

### P2
| Key | Title |
|---|---|
| `UMS-K8S-P2-001` | Standardise image names → ghcr.io scheme |
| `UMS-K8S-P2-002` | Fill ConfigMap __PATCH_REQUIRED__ sentinels |
| `UMS-K8S-P2-003` | Verify BFF vs ApiGateway deployment content |
| `UMS-CI-P2-001` | Add deployment approval gate (GitHub Environments) |
| `UMS-INFRA-P2-001` | Create .nexus.yaml + Nexus K8s provider |
| `UMS-TEST-P2-002` | Remove TestResults/*.trx + .gitignore rules |

## CRITICAL RULES (never break these)
```
• OutboxRelayService  →  Infrastructure layer ONLY (never API)
• Domain events       →  via RaiseDomainEvent() on AggregateRoot
• TenantId            →  inherited from BaseEntity, set at construction
• EF query filter     →  every DbContext MUST filter by TenantId
• ITenantContext      →  always injected, never hardcoded
• GHCR image format   →  ghcr.io/harshmaury/ums/<service>-api:<sha8>
• Kafka topics        →  KafkaTopics.* constants only, never string literals
• New .cs files       →  must have // Key: UMS-XXX header
```

## LAST SESSION — 2026-03-12
- Full tree audit completed → 27 keys identified
- SharedKernel new files: `BaseEntity`, `IDomainEvent`, `DomainEvent`, `AggregateRoot`
- Bug fixed: `OutboxRelayServiceBase` was publishing `TenantId = string.Empty` to Kafka
- `Directory.Build.props` has MimeKit CVE suppression — can remove once OutboxMessage migration verified
- `_backups/` contains second copy of compromised `secret.yaml` — must purge with `git-filter-repo`

## NEXT ACTION
```
1. Copy output files from UMS-SHARED-P0-001_P0-002_OUTPUT.zip into repo
2. git add + commit + push
3. Drop Domain/Common/ files for UMS-SHARED-P0-003
   → dropped UMS-SHARED-P0-003
```
