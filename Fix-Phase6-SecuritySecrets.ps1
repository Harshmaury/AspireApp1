# ============================================================
# Phase 6 — CRITICAL SECURITY: Remove secrets from git
# This phase already completed successfully.
# This stub confirms Phase 6 artifacts are in place.
# ============================================================

$root = "C:\Users\harsh\source\repos\AspireApp1"

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host " PHASE 6 — Security: Verifying Phase 6 artifacts" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan

$checks = @(
    "$root\k8s\base\secret.yaml.template",
    "$root\_ROTATE-CREDENTIALS.md",
    "$root\.gitleaks.toml",
    "$root\.git\hooks\pre-commit",
    "$root\k8s\base\sealed-secrets"
)

$allOk = $true
foreach ($item in $checks) {
    if (Test-Path $item) {
        Write-Host "  ✅ Found: $item" -ForegroundColor Green
    } else {
        Write-Host "  ⚠  Missing: $item" -ForegroundColor Yellow
        $allOk = $false
    }
}

if ($allOk) {
    Write-Host ""
    Write-Host "  Phase 6 artifacts verified. Secrets rotation steps still required." -ForegroundColor Green
    Write-Host "  See: $root\_ROTATE-CREDENTIALS.md" -ForegroundColor Cyan
} else {
    Write-Host ""
    Write-Host "  Some Phase 6 artifacts missing — re-run the original Fix-Phase6-SecuritySecrets script." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "  REMINDER — Mandatory steps not automated:" -ForegroundColor Red
Write-Host "  1. Rotate ALL credentials in k8s/base/secret.yaml NOW" -ForegroundColor Red
Write-Host "  2. git filter-repo --path k8s/base/secret.yaml --invert-paths --force" -ForegroundColor Red
Write-Host "  3. Force-push and notify team to re-clone" -ForegroundColor Red
