# UMS SESSION LOG & CONTEXT BOOTSTRAP
### Living Document — Update at end of every session

---

## HOW TO START A NEW CONTEXT WINDOW

### Step 1 — Paste this block first:
```
I am working on UMS — a multi-region multi-tenant University Management System.
Read DOC 1 (Infrastructure), DOC 2 (Phase Planning), DOC 3 (Session Log) before doing anything.
Do not generate any code until you confirm you understand the current state.
Ask me to paste any file content you need before proceeding.
Current working directory: C:\Users\harsh\source\repos\AspireApp1
All Minikube/kubectl commands are WSL Ubuntu (bash). All dotnet/PowerShell commands are Windows PowerShell.
```

### Step 2 — Paste DOC 1, DOC 2, DOC 3 in full.

### Step 3 — Ask:
```
What is the exact next command I should run?
```

---

## CURRENT STATE

**Date:** 2026-03-02
**Phase:** Phase 1 — Multi-Region Foundation
**Session:** Secret migration + cluster stabilization

### Where We Stopped

All Phase 1 HIGH priority tasks complete. Cluster fully healthy. Ready for MEDIUM tasks.

---

## CLUSTER STATE

- All 16 pods 1/1 Running
- Minikube: WSL Ubuntu, containerd runtime, docker driver
- REGION_ID=dev-local, REGION_ROLE=PRIMARY, REGION_WRITE_ALLOWED=true confirmed live
- DB credentials: Username=ums_user, Password=ums_pass_dev (in ums-secrets)
- Baseline snapshot: .ums/snapshots/baseline-20260302-154849.snap.json

---

## EXACT NEXT COMMANDS (Run in this order)
```
# 1. Check pods are still healthy
# [WSL] kubectl get pods -n ums

# 2. Find all Kafka consumer group usages in src/
# [WSL] grep -r "GroupId\|ConsumerGroup" /mnt/c/Users/harsh/source/repos/AspireApp1/src --include="*.cs" -l

# 3. Rename consumer groups to {svc}.{region}.{purpose} pattern

# 4. Run verify-region to confirm AGS-014 passes
# [PowerShell] dotnet run --project src/Cli/Ums.Cli -- verify-region --project src --format text

# 5. Add /health/region endpoint to all 9 services
```

---

## CRITICAL RUNTIME NOTES

- Minikube uses containerd runtime — DOCKER_BUILDKIT=0 docker build does NOT work
- Build images: use minikube image build OR WSL native docker build + minikube image load
- Always scale to 1 replica after every kubectl apply (HPAs spin up 2, CPU hits 96%)
- After apply always run: kubectl scale deployment -n ums --replicas=1 academic-api attendance-api examination-api faculty-api fee-api hostel-api identity-api notification-api student-api bff api-gateway
- New-Item / dotnet commands: PowerShell only
- kubectl / minikube commands: WSL Ubuntu only

---

## SECRET STRUCTURE (ums-secrets)

Contains:
- POSTGRES_USER: ums_user
- POSTGRES_PASSWORD: ums_pass_dev
- ConnectionStrings__{Service}Db (x9 RW)
- ConnectionStrings__{Service}DbReadOnly (x9 RO)
All pointing to: Host=postgres-0.postgres;Port=5432;Username=ums_user;Password=ums_pass_dev

---

## PHASE 1 TASK STATUS

| Task | Status |
|---|---|
| Fix build errors | DONE |
| verify-region PASS (AGS-014 + AGS-015) | DONE |
| All 16 pods Running | DONE |
| REGION_ID=dev-local confirmed live | DONE |
| DB passwords moved from ConfigMap to Secret | DONE |
| .ums/ folder initialized | DONE |
| Baseline snapshot captured | DONE |
| Rename Kafka consumer groups {svc}.{region}.{purpose} | NEXT |
| Add /health/region endpoint to all services | TODO |

---

## KNOWN ISSUES / LESSONS LEARNED

| Issue | Root Cause | Fix |
|---|---|---|
| minikube docker-env + containerd | Experimental, docker build fails | Use minikube image build or WSL native docker + minikube image load |
| TLS timeout on mcr.microsoft.com | Transient network | Retry — base images cache after first pull |
| Pods Pending after apply | 2 replicas, Minikube CPU 96% | Always scale to 1 after apply |
| Secret not picked up by running pods | K8s does not restart pods on secret change | kubectl delete pods -n ums -l app=name |
| $host reserved in PowerShell | Built-in variable | Use $pghost instead |
| snapshot create --format flag | Not supported | Remove --format flag |
| Resolve-Path fails if file does not exist | File must exist first | Use direct string path for new files |