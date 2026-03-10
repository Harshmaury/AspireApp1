# ============================================================
# Phase 4 — SharedKernel: Unified ValidationBehavior
# ============================================================

$ErrorActionPreference = "Stop"
$root   = "C:\Users\harsh\source\repos\AspireApp1"
$backup = "$root\_backups\phase4-validation-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
New-Item -ItemType Directory -Path $backup -Force | Out-Null

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host " PHASE 4 — SharedKernel: Unified ValidationBehavior" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan

$skProj = Get-ChildItem "$root\src" -Recurse -Filter "UMS.SharedKernel.csproj" |
    Where-Object { $_.FullName -notmatch "\\obj\\" } | Select-Object -First 1
if (-not $skProj) { Write-Host "  ERROR: SharedKernel not found" -ForegroundColor Red; exit 1 }

$skDir  = $skProj.DirectoryName
$appDir = "$skDir\Application"
New-Item -ItemType Directory -Path $appDir -Force | Out-Null

$vbPath = "$appDir\ValidationBehavior.cs"
if (-not (Test-Path $vbPath)) {
    Set-Content -Path $vbPath -Encoding UTF8 -Value @"
// UMS.SharedKernel/Application/ValidationBehavior.cs
//
// Unified MediatR pipeline behaviour — validates every IRequest<T>
// that has one or more registered FluentValidation validators.
// Replaces 9 per-service copies (with inconsistent naming).

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;

namespace UMS.SharedKernel.Application;

public sealed class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next();
    }
}
"@
    Write-Host "  [1/2] Created: SharedKernel\Application\ValidationBehavior.cs" -ForegroundColor Green
} else {
    Write-Host "  [1/2] ValidationBehavior.cs already exists — skipped" -ForegroundColor DarkGray
}

# ── Scan and back up per-service ValidationBehavior files ────
$services = @("Identity","Academic","Student","Attendance","Examination","Fee","Faculty","Hostel","Notification")
Write-Host ""
Write-Host "  [2/2] Backing up per-service ValidationBehavior files:" -ForegroundColor Yellow
$count = 0
foreach ($svc in $services) {
    $files = Get-ChildItem "$root\src\Services\$svc" -Recurse -Include "*ValidationBehaviour*.cs","*ValidationBehavior*.cs" -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notmatch "\\obj\\" }
    foreach ($f in $files) {
        Copy-Item $f.FullName "$backup\$svc-$(Split-Path $f -Leaf)" -ErrorAction SilentlyContinue
        Write-Host "    Backed up: $($f.FullName)" -ForegroundColor DarkGray
        Write-Host "    ACTION: Delete this file; register UMS.SharedKernel ValidationBehavior in DI instead." -ForegroundColor Cyan
        $count++
    }
}
if ($count -eq 0) { Write-Host "    None found — may already be consolidated." -ForegroundColor DarkGray }

Write-Host ""
Write-Host "  Phase 4 complete." -ForegroundColor Green
