using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text.Json;
using UMS.SharedKernel.Domain;

namespace UMS.SharedKernel.Infrastructure;

public abstract class DomainEventDispatcherInterceptorBase : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ConvertDomainEventsToOutbox(eventData.Context);
        return ValueTask.FromResult(result);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ConvertDomainEventsToOutbox(eventData.Context);
        return result;
    }

    private static void ConvertDomainEventsToOutbox(DbContext? context)
    {
        if (context is null) return;

        var aggregates = context.ChangeTracker
            .Entries<IAggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Count != 0)
            .Select(e => e.Entity)
            .ToList();

        if (aggregates.Count == 0) return;

        var outboxRows = aggregates
            .SelectMany(a => a.DomainEvents)
            .Select(evt => new OutboxMessage
            {
                Id         = Guid.NewGuid(),
                EventType  = evt.GetType().FullName ?? evt.GetType().Name,
                Payload    = JsonSerializer.Serialize(evt, evt.GetType()),
                TenantId   = (evt as Kafka.ITenantedEvent)?.TenantId.ToString(),
                OccurredAt = DateTimeOffset.UtcNow,
                CreatedAt  = DateTimeOffset.UtcNow,
            })
            .ToList();

        context.Set<OutboxMessage>().AddRange(outboxRows);

        foreach (var aggregate in aggregates)
            aggregate.ClearDomainEvents();
    }
}
