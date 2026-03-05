using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Student.Domain.Common;
using System.Text.Json;
using UMS.SharedKernel.Kafka;

namespace Student.Infrastructure.Persistence;

public sealed class DomainEventDispatcherInterceptor : SaveChangesInterceptor
{
    private readonly IMediator _mediator;
    private readonly string _regionOrigin;

    public DomainEventDispatcherInterceptor(IMediator mediator, IConfiguration configuration)
    {
        _mediator = mediator;
        _regionOrigin = configuration["REGION_ID"] ?? "unknown";
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        if (eventData.Context is StudentDbContext context)
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

    private void WriteOutboxMessages(StudentDbContext context)
    {
        var events = context.ChangeTracker
            .Entries<IAggregateRoot>()
            .SelectMany(e => e.Entity.DomainEvents)
            .OfType<ITenantedEvent>()
            .ToList();

        var outboxMessages = events.Select(e =>
        {
            var rawPayload = JsonSerializer.Serialize(e, e.GetType());
            var envelope   = KafkaEventEnvelope.Create(
                eventType:    e.GetType().Name,
                tenantId:     e.TenantId,
                regionOrigin: _regionOrigin,
                payload:      rawPayload);
            return OutboxMessage.Create(
                eventType: e.GetType().FullName!,
                payload:   JsonSerializer.Serialize(envelope));
        }).ToList();

        context.OutboxMessages.AddRange(outboxMessages);
    }

    private async Task DispatchDomainEventsAsync(DbContext context, CancellationToken ct)
    {
        var aggregates = context.ChangeTracker
            .Entries<IAggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var events = aggregates.SelectMany(a => a.DomainEvents).ToList();
        aggregates.ForEach(a => a.ClearDomainEvents());

        foreach (var domainEvent in events)
            await _mediator.Publish(domainEvent, ct);
    }
}
