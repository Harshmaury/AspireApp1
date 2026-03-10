// UMS.SharedKernel/Infrastructure/DomainEventDispatcherInterceptorBase.cs
//
// EF Core SaveChanges interceptor — dispatches domain events after
// each SaveChanges call. Inherit in every service's Infrastructure layer.
// Uses the correct EF Core SaveChangesInterceptor API (EF Core 7+/8+/10).

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using UMS.SharedKernel;

namespace UMS.SharedKernel.Infrastructure;

/// <summary>
/// Dispatches domain events raised on aggregates after EF Core SaveChanges.
/// Inherit in each service: class MyInterceptor : DomainEventDispatcherInterceptorBase { }
/// </summary>
public abstract class DomainEventDispatcherInterceptorBase : SaveChangesInterceptor
{
    private readonly IPublisher _publisher;

    protected DomainEventDispatcherInterceptorBase(IPublisher publisher)
        => _publisher = publisher;

    // ── Async path ────────────────────────────────────────────
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await DispatchEventsAsync(eventData.Context, cancellationToken);
        return result;
    }

    // ── Sync path ─────────────────────────────────────────────
    public override int SavedChanges(
        SaveChangesCompletedEventData eventData,
        int result)
    {
        DispatchEventsAsync(eventData.Context, CancellationToken.None)
            .GetAwaiter().GetResult();
        return result;
    }

    // ── Core dispatch logic ───────────────────────────────────
    private async Task DispatchEventsAsync(DbContext? context, CancellationToken ct)
    {
        if (context is null) return;

        var aggregates = context.ChangeTracker
            .Entries<IAggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var events = aggregates
            .SelectMany(a => a.DomainEvents)
            .ToList();

        aggregates.ForEach(a => a.ClearDomainEvents());

        foreach (var evt in events)
            await _publisher.Publish(evt, ct);
    }
}

