$ErrorActionPreference = "Stop"
$root = "C:\Users\harsh\source\repos\AspireApp1"

$target = Get-ChildItem $root -Recurse -Filter "DbFactory.cs" |
    Where-Object { $_.FullName -notmatch "\\bin\\" -and $_.FullName -notmatch "\\obj\\" -and $_.FullName -notmatch "\\Script\\" } |
    Select-Object -First 1

if (-not $target) {
    Write-Host "ERROR: DbFactory.cs not found" -ForegroundColor Red
    exit 1
}

Write-Host "Target: $($target.FullName)" -ForegroundColor Green

$backup = "$($target.FullName).bak_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
Copy-Item $target.FullName $backup
Write-Host "Backup: $backup" -ForegroundColor DarkGray

Copy-Item "$root\Script\DbFactory.cs" $target.FullName -Force
Write-Host "Done. DbFactory replaced." -ForegroundColor Green
Write-Host ""
Write-Host "Run: dotnet test --filter TenantIsolation" -ForegroundColor Cyan
