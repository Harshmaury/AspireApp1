# ============================================================
# Phase 2 — SharedKernel: DomainEventDispatcherInterceptorBase
# ============================================================

$ErrorActionPreference = "Stop"
$root   = "C:\Users\harsh\source\repos\AspireApp1"
$backup = "$root\_backups\phase2-interceptor-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
New-Item -ItemType Directory -Path $backup -Force | Out-Null

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host " PHASE 2 — SharedKernel: DomainEventDispatcherInterceptorBase" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan

$skProj = Get-ChildItem "$root\src" -Recurse -Filter "UMS.SharedKernel.csproj" |
    Where-Object { $_.FullName -notmatch "\\obj\\" } | Select-Object -First 1

if (-not $skProj) { Write-Host "  ERROR: SharedKernel not found" -ForegroundColor Red; exit 1 }

$skDir    = $skProj.DirectoryName
$infraDir = "$skDir\Infrastructure"
New-Item -ItemType Directory -Path $infraDir -Force | Out-Null

$interceptorPath = "$infraDir\DomainEventDispatcherInterceptorBase.cs"
if (-not (Test-Path $interceptorPath)) {
    Set-Content -Path $interceptorPath -Encoding UTF8 -Value @"
// UMS.SharedKernel/Infrastructure/DomainEventDispatcherInterceptorBase.cs
//
// EF Core SaveChanges interceptor that dispatches domain events
// raised on aggregates. Each service inherits this — no per-service copy needed.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using UMS.SharedKernel.Domain;

namespace UMS.SharedKernel.Infrastructure;

public abstract class DomainEventDispatcherInterceptorBase : SaveChangesInterceptor
{
    private readonly IPublisher _publisher;

    protected DomainEventDispatcherInterceptorBase(IPublisher publisher)
        => _publisher = publisher;

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await DispatchDomainEventsAsync(eventData.Context, cancellationToken);
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        DispatchDomainEventsAsync(eventData.Context, default).GetAwaiter().GetResult();
        return base.SavedChanges(eventData, result);
    }

    private async Task DispatchDomainEventsAsync(DbContext? context, CancellationToken ct)
    {
        if (context is null) return;

        var aggregates = context.ChangeTracker
            .Entries<IAggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = aggregates
            .SelectMany(a => a.DomainEvents)
            .ToList();

        aggregates.ForEach(a => a.ClearDomainEvents());

        foreach (var evt in domainEvents)
            await _publisher.Publish(evt, ct);
    }
}
"@
    Write-Host "  Created: SharedKernel\Infrastructure\DomainEventDispatcherInterceptorBase.cs" -ForegroundColor Green
} else {
    Write-Host "  DomainEventDispatcherInterceptorBase.cs already exists — skipped" -ForegroundColor DarkGray
}

# ── Report per-service interceptor files ─────────────────────
$services = @("Identity","Academic","Student","Attendance","Examination","Fee","Faculty","Hostel","Notification")
Write-Host ""
Write-Host "  Services with interceptor to consolidate:" -ForegroundColor Yellow
foreach ($svc in $services) {
    $interceptor = Get-ChildItem "$root\src\Services\$svc" -Recurse -Filter "*Interceptor*.cs" -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notmatch "\\obj\\" }
    foreach ($f in $interceptor) {
        Copy-Item $f.FullName "$backup\$svc-$(Split-Path $f.FullName -Leaf)" -ErrorAction SilentlyContinue
        Write-Host "    $($f.FullName)" -ForegroundColor DarkGray
    }
}

Write-Host ""
Write-Host "  Phase 2 complete." -ForegroundColor Green
