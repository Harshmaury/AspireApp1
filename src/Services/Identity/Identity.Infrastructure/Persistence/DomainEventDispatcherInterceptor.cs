// src/Services/Identity/Identity.Infrastructure/Persistence/DomainEventDispatcherInterceptor.cs
using Identity.Domain.Common;
using UMS.SharedKernel.Kafka;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text.Json;

namespace Identity.Infrastructure.Persistence;

public sealed class DomainEventDispatcherInterceptor : SaveChangesInterceptor
{
    private readonly IMediator _mediator;

    public DomainEventDispatcherInterceptor(IMediator mediator)
        => _mediator = mediator;

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        if (eventData.Context is ApplicationDbContext context)
            WriteOutboxMessages(context);

        return base.SavingChangesAsync(eventData, result, ct);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken ct = default)
    {
        if (eventData.Context is not null)
            await DispatchDomainEventsAsync(eventData.Context, ct);

        return await base.SavedChangesAsync(eventData, result, ct);
    }

    private static void WriteOutboxMessages(ApplicationDbContext context)
    {
        var events = context.ChangeTracker
            .Entries<IAggregateRoot>()
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        // BUG-001 FIX: Extract TenantId from ITenantedEvent â€” was always Guid.Empty before
        var outboxMessages = events.Select(e => OutboxMessage.Create(
            eventType: e.GetType().FullName!,
            payload: JsonSerializer.Serialize(e, e.GetType()),
            tenantId: e is ITenantedEvent te ? te.TenantId : Guid.Empty
        )).ToList();

        context.OutboxMessages.AddRange(outboxMessages);
    }

    private async Task DispatchDomainEventsAsync(DbContext context, CancellationToken ct)
    {
        var aggregates = context.ChangeTracker
            .Entries<IAggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var events = aggregates
            .SelectMany(a => a.DomainEvents)
            .ToList();

        aggregates.ForEach(a => a.ClearDomainEvents());

        foreach (var domainEvent in events)
            await _mediator.Publish(domainEvent, ct);
    }
}
