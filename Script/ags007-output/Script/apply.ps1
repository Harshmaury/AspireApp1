Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$Root    = "C:\Users\harsh\source\repos\AspireApp1"
$Patches = "$PSScriptRoot\..\DbContexts"

function Info ([string]$msg) { Write-Host "  $msg" -ForegroundColor Cyan }
function Ok   ([string]$msg) { Write-Host "  OK $msg" -ForegroundColor Green }
function Warn ([string]$msg) { Write-Host "  WARN $msg" -ForegroundColor Yellow }
function Fail ([string]$msg) { Write-Host "  FAIL $msg" -ForegroundColor Red; exit 1 }

$DbContextMap = @{
    "AcademicDbContext.cs"     = "src\Services\Academic\Academic.Infrastructure\Persistence\AcademicDbContext.cs"
    "StudentDbContext.cs"      = "src\Services\Student\Student.Infrastructure\Persistence\StudentDbContext.cs"
    "AttendanceDbContext.cs"   = "src\Services\Attendance\Attendance.Infrastructure\Persistence\AttendanceDbContext.cs"
    "ExaminationDbContext.cs"  = "src\Services\Examination\Examination.Infrastructure\Persistence\ExaminationDbContext.cs"
    "FacultyDbContext.cs"      = "src\Services\Faculty\Faculty.Infrastructure\Persistence\FacultyDbContext.cs"
    "FeeDbContext.cs"          = "src\Services\Fee\Fee.Infrastructure\Persistence\FeeDbContext.cs"
    "HostelDbContext.cs"       = "src\Services\Hostel\Hostel.Infrastructure\Persistence\HostelDbContext.cs"
    "NotificationDbContext.cs" = "src\Services\Notification\Notification.Infrastructure\Persistence\NotificationDbContext.cs"
}

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  AGS-007 Tenant Isolation Fix" -ForegroundColor Cyan
Write-Host "  Repo : $Root" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "[ STEP 1 ] Validating paths..." -ForegroundColor White
if (-not (Test-Path $Root))    { Fail "Repo root not found: $Root" }
if (-not (Test-Path $Patches)) { Fail "Patches folder not found: $Patches" }
foreach ($file in $DbContextMap.Keys) {
    if (-not (Test-Path (Join-Path $Patches $file))) { Fail "Patch file missing: $file" }
}
Ok "All 8 patch files found."

Write-Host ""
Write-Host "[ STEP 2 ] Backing up and applying patches..." -ForegroundColor White
$applied = 0
foreach ($file in $DbContextMap.Keys) {
    $targetPath = Join-Path $Root $DbContextMap[$file]
    $patchFile  = Join-Path $Patches $file
    $backupPath = "$targetPath.bak"
    if (-not (Test-Path $targetPath)) { Warn "Target not found, skipping: $file"; continue }
    Copy-Item $targetPath $backupPath -Force
    Copy-Item $patchFile  $targetPath -Force
    Ok "Patched: $file"
    $applied++
}
Info "Applied $applied of $($DbContextMap.Count) patches."

Write-Host ""
Write-Host "[ STEP 3 ] Building solution..." -ForegroundColor White
& dotnet build "$Root\AspireApp1.slnx" -c Release --nologo
if ($LASTEXITCODE -ne 0) {
    Write-Host "BUILD FAILED - rolling back..." -ForegroundColor Red
    foreach ($file in $DbContextMap.Keys) {
        $targetPath = Join-Path $Root $DbContextMap[$file]
        $backupPath = "$targetPath.bak"
        if (Test-Path $backupPath) { Copy-Item $backupPath $targetPath -Force; Info "Rolled back: $file" }
    }
    Fail "Build failed. All patches rolled back."
}
Ok "Build passed."

Write-Host ""
Write-Host "[ STEP 4 ] Running Identity unit tests..." -ForegroundColor White
& dotnet test "$Root\src\Services\Identity\Identity.Tests" --nologo
if ($LASTEXITCODE -ne 0) { Fail "Identity tests failed." }
Ok "Identity tests passed."

Write-Host ""
Write-Host "[ STEP 5 ] Running Aegis governance..." -ForegroundColor White
& dotnet run --project "$Root\src\Cli\Ums.Cli" -- govern verify all

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  COMPLETE - patches applied: $applied of $($DbContextMap.Count)" -ForegroundColor Green
Write-Host "  Check governance output above - AGS-007 must show 0 warnings" -ForegroundColor Yellow
Write-Host "============================================================" -ForegroundColor Cyan
