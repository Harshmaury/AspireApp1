$ErrorActionPreference = "Stop"
$root = "C:\Users\harsh\source\repos\AspireApp1"

Write-Host "AGS-007 Fix - locating TenantIsolationRule.cs..." -ForegroundColor Cyan

$target = Get-ChildItem $root -Recurse -Filter "TenantIsolationRule.cs" |
    Where-Object { $_.FullName -notmatch "\\bin\\" -and $_.FullName -notmatch "\\obj\\" } |
    Select-Object -First 1

if (-not $target) {
    Write-Host "ERROR: TenantIsolationRule.cs not found." -ForegroundColor Red
    exit 1
}

Write-Host "Target : $($target.FullName)" -ForegroundColor Green

$backup = "$($target.FullName).bak_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
Copy-Item $target.FullName $backup
Write-Host "Backup : $backup" -ForegroundColor DarkGray

$source = "$root\Script\TenantIsolationRule.cs"
if (-not (Test-Path $source)) {
    Write-Host "ERROR: $source not found. Place TenantIsolationRule.cs in the Script folder first." -ForegroundColor Red
    exit 1
}

Copy-Item $source $target.FullName -Force
Write-Host "Done. Rule replaced successfully." -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  dotnet build"
Write-Host "  dotnet test --filter TenantIsolation"
