// UMS â€” University Management System
// Key:     UMS-SHARED-P0-002
// Service: SharedKernel
// Layer:   Infrastructure
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
        int                           result,
        CancellationToken             cancellationToken = default)
    {
        await DispatchEventsAsync(eventData.Context, cancellationToken);
        return result;
    }

    public override int SavedChanges(
        SaveChangesCompletedEventData eventData,
        int                           result)
    {
        DispatchEventsAsync(eventData.Context, CancellationToken.None)
            .GetAwaiter().GetResult();
        return result;
    }

    private async Task DispatchEventsAsync(DbContext? context, CancellationToken ct)
    {
        if (context is null) return;

        var aggregates = context.ChangeTracker
            .Entries<IAggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Count != 0)
            .Select(e => e.Entity)
            .ToList();

        var events = aggregates.SelectMany(a => a.DomainEvents).ToList();
        foreach (var aggregate in aggregates) aggregate.ClearDomainEvents();
        foreach (var evt in events) await _publisher.Publish(evt, ct);
    }
}
