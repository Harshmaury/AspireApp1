# ─────────────────────────────────────────────────────────────
# umsv — UMS Session Verifier
# Run after every commit to confirm session state.
# Add to ums-aliases.ps1 so it's available as: umsv
# ─────────────────────────────────────────────────────────────

function umsv {
    $repo   = "C:\Users\harsh\source\repos\AspireApp1"
    $drop   = "C:\Users\harsh\Downloads\ums-drop"
    $remote = "https://raw.githubusercontent.com/Harshmaury/AspireApp1/main/UMS-LOG.md"

    Push-Location $repo

    # ── Git state ────────────────────────────────────────────
    $branch   = git rev-parse --abbrev-ref HEAD 2>$null
    $sha      = git rev-parse --short HEAD 2>$null
    $lastMsg  = git log -1 --pretty=format:"%s" 2>$null
    $status   = git status --porcelain 2>$null
    $dirty    = if ($status) { "dirty ($(@($status).Count) file(s))" } else { "clean" }
    $date     = Get-Date -Format "yyyyMMdd"
    $sessionKey = "UMS-$sha-$date"

    # ── Active key from UMS-LOG.md ───────────────────────────
    $logFile   = "$repo\UMS-LOG.md"
    $activeKey = "none"
    $nextAction = "none"
    if (Test-Path $logFile) {
        $logLines = Get-Content $logFile
        $inProgress = $logLines | Where-Object { $_ -match "\| `\`UMS-" -and $_ -match "🔄" }
        if ($inProgress) {
            $activeKey = ($inProgress[0] -replace '.*`(UMS-[^`]+)`.*','$1').Trim()
        }
        $nextIdx = ($logLines | Select-String "## NEXT ACTION").LineNumber
        if ($nextIdx) {
            $nextAction = ($logLines[$nextIdx] -replace '^[0-9]+\.\s*','').Trim()
        }
    }

    # ── Open keys count from UMS-LOG.md ─────────────────────
    $p0Open = ($logLines | Where-Object { $_ -match "UMS-.*P0" -and $_ -match "\|$" }).Count
    $p1Open = ($logLines | Where-Object { $_ -match "UMS-.*P1" -and $_ -match "\|$" }).Count
    $p2Open = ($logLines | Where-Object { $_ -match "UMS-.*P2" -and $_ -match "\|$" }).Count

    # ── Drop zone check ──────────────────────────────────────
    $dropExists = if (Test-Path $drop) { "ready" } else { "MISSING" }

    # ── Build check (fast — just check last build output) ────
    $buildOk = if (Test-Path "$repo\obj") { "see last run" } else { "not built" }

    # ── Render ───────────────────────────────────────────────
    $w = 54
    $line = "─" * $w

    Write-Host ""
    Write-Host "╔══════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║           UMS SESSION VERIFY  v1.0                  ║" -ForegroundColor Cyan
    Write-Host "╚══════════════════════════════════════════════════════╝" -ForegroundColor Cyan

    Write-Host "  SESSION KEY  │ " -NoNewline -ForegroundColor DarkGray
    Write-Host $sessionKey -ForegroundColor Yellow

    Write-Host "  LOG         │ " -NoNewline -ForegroundColor DarkGray
    Write-Host $remote -ForegroundColor DarkCyan

    Write-Host "  WORKFLOW    │ " -NoNewline -ForegroundColor DarkGray
    Write-Host "https://raw.githubusercontent.com/Harshmaury/AspireApp1/main/UMS-WORKFLOW.md" -ForegroundColor DarkCyan

    Write-Host ""
    Write-Host "── GIT $line".Substring(0, $w + 5) -ForegroundColor DarkGray
    Write-Host "  branch      " -NoNewline -ForegroundColor DarkGray; Write-Host $branch -ForegroundColor Green
    Write-Host "  sha         " -NoNewline -ForegroundColor DarkGray; Write-Host $sha -ForegroundColor Green
    Write-Host "  last        " -NoNewline -ForegroundColor DarkGray; Write-Host $lastMsg -ForegroundColor White
    Write-Host "  status      " -NoNewline -ForegroundColor DarkGray
    if ($dirty -eq "clean") { Write-Host $dirty -ForegroundColor Green }
    else                    { Write-Host $dirty -ForegroundColor Red }

    Write-Host ""
    Write-Host "── KEYS $line".Substring(0, $w + 5) -ForegroundColor DarkGray
    Write-Host "  active      " -NoNewline -ForegroundColor DarkGray
    if ($activeKey -eq "none") { Write-Host $activeKey -ForegroundColor DarkGray }
    else                       { Write-Host $activeKey -ForegroundColor Yellow }

    Write-Host "  open P0     " -NoNewline -ForegroundColor DarkGray
    if ($p0Open -gt 0) { Write-Host $p0Open -ForegroundColor Red } else { Write-Host "0" -ForegroundColor Green }

    Write-Host "  open P1     " -NoNewline -ForegroundColor DarkGray
    Write-Host $p1Open -ForegroundColor Yellow

    Write-Host "  open P2     " -NoNewline -ForegroundColor DarkGray
    Write-Host $p2Open -ForegroundColor Cyan

    Write-Host ""
    Write-Host "── ENV $line".Substring(0, $w + 5) -ForegroundColor DarkGray
    Write-Host "  drop zone   " -NoNewline -ForegroundColor DarkGray
    if ($dropExists -eq "ready") { Write-Host $dropExists -ForegroundColor Green }
    else                         { Write-Host $dropExists -ForegroundColor Red }
    Write-Host "  repo        " -NoNewline -ForegroundColor DarkGray; Write-Host $repo -ForegroundColor DarkGray
    Write-Host "  dotnet      " -NoNewline -ForegroundColor DarkGray; Write-Host (dotnet --version 2>$null) -ForegroundColor DarkGray

    Write-Host ""
    Write-Host "── NEXT $line".Substring(0, $w + 5) -ForegroundColor DarkGray
    Write-Host "  $nextAction" -ForegroundColor White
    Write-Host ""

    Pop-Location
}
