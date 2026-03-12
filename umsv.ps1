function umsv {
    $repo   = "C:\Users\harsh\source\repos\AspireApp1"
    $drop   = "C:\Users\harsh\Downloads\ums-drop"

    Push-Location $repo

    $branch  = git rev-parse --abbrev-ref HEAD 2>$null
    $sha     = git rev-parse --short HEAD 2>$null
    $lastMsg = git log -1 --pretty=format:"%s" 2>$null
    $dirty   = if (git status --porcelain 2>$null) { "dirty" } else { "clean" }
    $date    = Get-Date -Format "yyyyMMdd"
    $key     = "UMS-$sha-$date"
    $dotnet  = dotnet --version 2>$null
    $dropOk  = Test-Path $drop

    $logFile    = "$repo\UMS-LOG.md"
    $nextAction = "see UMS-LOG.md"
    $p0 = 0; $p1 = 0; $p2 = 0

    if (Test-Path $logFile) {
        $lines = Get-Content $logFile
        $p0 = @($lines | Where-Object { $_ -match "UMS-" -and $_ -match "OPEN" -and $_ -match "P0" }).Count
        $p1 = @($lines | Where-Object { $_ -match "UMS-" -and $_ -match "OPEN" -and $_ -match "P1" }).Count
        $p2 = @($lines | Where-Object { $_ -match "UMS-" -and $_ -match "OPEN" -and $_ -match "P2" }).Count
        $nextIdx = ($lines | Select-String "## NEXT ACTION" | Select-Object -First 1).LineNumber
        if ($nextIdx) { $nextAction = $lines[$nextIdx].TrimStart("0123456789. ") }
    }

    Write-Host ""
    Write-Host "+------------------------------------------------------+" -ForegroundColor Cyan
    Write-Host "|          UMS SESSION VERIFY  v1.0                   |" -ForegroundColor Cyan
    Write-Host "+------------------------------------------------------+" -ForegroundColor Cyan
    Write-Host ("  SESSION KEY  | " + $key) -ForegroundColor Yellow
    Write-Host "  LOG          | github.com/Harshmaury/AspireApp1/blob/main/UMS-LOG.md"
    Write-Host "  WORKFLOW     | github.com/Harshmaury/AspireApp1/blob/main/UMS-WORKFLOW.md"
    Write-Host ""
    Write-Host "-- GIT ------------------------------------------------" -ForegroundColor DarkGray
    Write-Host ("  branch       " + $branch) -ForegroundColor Green
    Write-Host ("  last         " + $sha + "  " + $lastMsg)
    if ($dirty -eq "clean") {
        Write-Host "  status       clean" -ForegroundColor Green
    } else {
        Write-Host "  status       dirty" -ForegroundColor Red
    }
    Write-Host ""
    Write-Host "-- KEYS -----------------------------------------------" -ForegroundColor DarkGray
    if ($p0 -gt 0) { Write-Host ("  open P0      " + $p0) -ForegroundColor Red }
    else           { Write-Host "  open P0      0" -ForegroundColor Green }
    Write-Host ("  open P1      " + $p1) -ForegroundColor Yellow
    Write-Host ("  open P2      " + $p2) -ForegroundColor Cyan
    Write-Host ""
    Write-Host "-- ENV ------------------------------------------------" -ForegroundColor DarkGray
    Write-Host ("  dotnet       " + $dotnet)
    if ($dropOk) { Write-Host "  drop zone    ready" -ForegroundColor Green }
    else         { Write-Host "  drop zone    MISSING" -ForegroundColor Red }
    Write-Host ("  repo         " + $repo)
    Write-Host ""
    Write-Host "-- NEXT -----------------------------------------------" -ForegroundColor DarkGray
    Write-Host ("  " + $nextAction)
    Write-Host ""

    Pop-Location
}
