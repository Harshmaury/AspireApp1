<#
.SYNOPSIS
    Start the full UMS stack in one command.
    Runs: Minikube → port-forwards → Aspire AppHost (dev mode)
.PARAMETER Mode
    dev   — Aspire AppHost only (no Minikube), fastest for feature development
    k8s   — Full Minikube stack
    stop  — Stop everything
.EXAMPLE
    Start-UMS.ps1 -Mode dev
    Start-UMS.ps1 -Mode k8s
    Start-UMS.ps1 -Mode stop
#>
#Requires -Version 7.0
param(
    [ValidateSet("dev","k8s","stop")]
    [string]$Mode = "dev"
)

$root    = "C:\Users\harsh\source\repos\AspireApp1"
$appHost = "$root\src\AppHost"

function Write-Step([string]$msg) {
    Write-Host "`n── $msg " -ForegroundColor Cyan -NoNewline
    Write-Host ("─" * [Math]::Max(1, 50 - $msg.Length)) -ForegroundColor DarkGray
}

function Assert-Built {
    Write-Step "Verifying build"
    Set-Location $root
    dotnet build UMS.slnx --no-incremental -v q
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Build failed — fix errors before starting." -ForegroundColor Red
        exit 1
    }
    Write-Host "✅ Build clean" -ForegroundColor Green
}

# ── STOP ──────────────────────────────────────────────────────────────────────
if ($Mode -eq "stop") {
    Write-Step "Stopping all UMS processes"

    # Kill Aspire AppHost
    Get-Process -Name "dotnet" -ErrorAction SilentlyContinue |
        Where-Object { $_.MainWindowTitle -match "AppHost" -or $_.CommandLine -match "AppHost" } |
        ForEach-Object { $_.Kill(); Write-Host "  Stopped: AppHost (PID $($_.Id))" }

    # Kill port-forwards
    Get-Process -Name "kubectl" -ErrorAction SilentlyContinue |
        ForEach-Object { $_.Kill(); Write-Host "  Stopped: kubectl (PID $($_.Id))" }

    Write-Host "`n✅ Everything stopped." -ForegroundColor Green
    exit 0
}

# ── DEV MODE — Aspire only ────────────────────────────────────────────────────
if ($Mode -eq "dev") {
    Assert-Built

    Write-Step "Starting Aspire AppHost (dev mode)"
    Write-Host "  Dashboard will open at: https://localhost:15888" -ForegroundColor DarkGray
    Write-Host "  Press Ctrl+C to stop`n" -ForegroundColor DarkGray

    Set-Location $appHost
    dotnet run --project AspireApp1.AppHost.csproj `
        --launch-profile "http" `
        -- --environment Development

    exit 0
}

# ── K8S MODE — Full Minikube stack ────────────────────────────────────────────
if ($Mode -eq "k8s") {
    Assert-Built

    # 1. Start Minikube if not running
    Write-Step "Checking Minikube"
    $mkStatus = minikube status --format "{{.Host}}" 2>$null
    if ($mkStatus -ne "Running") {
        Write-Host "  Starting Minikube..." -ForegroundColor Yellow
        minikube start --cpus 4 --memory 8192 --driver docker
        if ($LASTEXITCODE -ne 0) {
            Write-Host "❌ Minikube failed to start." -ForegroundColor Red; exit 1
        }
    } else {
        Write-Host "  Minikube already running ✅" -ForegroundColor Green
    }

    # 2. Verify UMS namespace
    Write-Step "Verifying K8s namespace"
    kubectl get namespace ums 2>$null
    if ($LASTEXITCODE -ne 0) {
        kubectl create namespace ums
        Write-Host "  Created namespace: ums" -ForegroundColor Yellow
    } else {
        Write-Host "  Namespace ums exists ✅" -ForegroundColor Green
    }

    # 3. Check pods
    Write-Step "Pod status"
    kubectl get pods -n ums

    # 4. Port-forwards in background jobs
    Write-Step "Starting port-forwards"
    $forwards = @(
        @{ Name = "identity-api";     Local = 5001; Remote = 80 },
        @{ Name = "student-api";      Local = 5002; Remote = 80 },
        @{ Name = "academic-api";     Local = 5003; Remote = 80 },
        @{ Name = "attendance-api";   Local = 5004; Remote = 80 },
        @{ Name = "examination-api";  Local = 5005; Remote = 80 },
        @{ Name = "fee-api";          Local = 5006; Remote = 80 },
        @{ Name = "hostel-api";       Local = 5007; Remote = 80 },
        @{ Name = "notification-api"; Local = 5008; Remote = 80 },
        @{ Name = "faculty-api";      Local = 5009; Remote = 80 },
        @{ Name = "api-gateway";      Local = 8080; Remote = 80 }
    )

    foreach ($fwd in $forwards) {
        $job = Start-Job -ScriptBlock {
            param($svc, $local, $remote)
            kubectl port-forward "svc/$svc" "${local}:${remote}" -n ums 2>$null
        } -ArgumentList $fwd.Name, $fwd.Local, $fwd.Remote

        Write-Host "  ✅ $($fwd.Name): localhost:$($fwd.Local)" -ForegroundColor Green
    }

    Write-Host ""
    Write-Host "🚀 UMS stack running:" -ForegroundColor Green
    Write-Host "   API Gateway  : http://localhost:8080" -ForegroundColor White
    Write-Host "   Identity API : http://localhost:5001" -ForegroundColor White
    Write-Host ""
    Write-Host "Run governance check:" -ForegroundColor DarkGray
    Write-Host "   dotnet run --project src\Cli\Ums.Cli\Ums.Cli.csproj -- govern verify all --project ." -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "Stop everything: Start-UMS.ps1 -Mode stop" -ForegroundColor DarkGray

    exit 0
}
