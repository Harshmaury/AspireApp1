# ============================================================
# Phase 7 — Housekeeping: Verify previously applied fixes
# ============================================================

$root = "C:\Users\harsh\source\repos\AspireApp1"

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host " PHASE 7 — Housekeeping: Verifying Phase 7 artifacts" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan

$checks = @(
    @{ Path="$root\UMS.slnx";                                            Label="Solution renamed to UMS.slnx"           }
    @{ Path="$root\k8s\overlays\dev-local\enable-metrics-server.sh";     Label="metrics-server enable script"           }
    @{ Path="$root\.gitleaks.toml";                                       Label=".gitleaks.toml present"                 }
)

foreach ($c in $checks) {
    if (Test-Path $c.Path) {
        Write-Host "  ✅ $($c.Label)" -ForegroundColor Green
    } else {
        Write-Host "  ⚠  Missing: $($c.Label) — $($c.Path)" -ForegroundColor Yellow
    }
}

# Verify Student.Infrastructure.csproj deduplication
$studentInfra = Get-ChildItem "$root\src\Services\Student" -Recurse -Filter "Student.Infrastructure.csproj" |
    Where-Object { $_.FullName -notmatch "\\obj\\" } | Select-Object -First 1
if ($studentInfra) {
    [xml]$doc   = Get-Content $studentInfra.FullName
    $ivtCount   = $doc.SelectNodes("//InternalsVisibleTo[@Include='TenantIsolation.Tests']").Count
    if ($ivtCount -le 1) {
        Write-Host "  ✅ Student.Infrastructure.csproj InternalsVisibleTo deduplicated ($ivtCount entry)" -ForegroundColor Green
    } else {
        Write-Host "  ⚠  Student.Infrastructure.csproj still has $ivtCount InternalsVisibleTo entries" -ForegroundColor Yellow
    }
}

# Verify CI coverage threshold
$ciYml = "$root\.github\workflows\ci.yml"
if (Test-Path $ciYml) {
    $content = Get-Content $ciYml -Raw
    if ($content -match "THRESHOLD|coverage.threshold|Enforce coverage") {
        Write-Host "  ✅ ci.yml coverage threshold present" -ForegroundColor Green
    } else {
        Write-Host "  ⚠  ci.yml coverage threshold missing" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "  Phase 7 verification complete." -ForegroundColor Green
