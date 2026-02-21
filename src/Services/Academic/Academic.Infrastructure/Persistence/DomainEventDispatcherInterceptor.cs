using Academic.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text.Json;
namespace Academic.Infrastructure.Persistence;
public sealed class DomainEventDispatcherInterceptor : SaveChangesInterceptor
{
    private readonly IMediator _mediator;
    public DomainEventDispatcherInterceptor(IMediator mediator) => _mediator = mediator;
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        if (eventData.Context is AcademicDbContext context)
            WriteOutboxMessages(context);
        return base.SavingChangesAsync(eventData, result, ct);
    }
    public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken ct = default)
    {
        if (eventData.Context is not null)
            await DispatchDomainEventsAsync(eventData.Context, ct);
        return await base.SavedChangesAsync(eventData, result, ct);
    }
    private static void WriteOutboxMessages(AcademicDbContext context)
    {
        var events = context.ChangeTracker.Entries<IAggregateRoot>().SelectMany(e => e.Entity.DomainEvents).ToList();
        var messages = events.Select(e => OutboxMessage.Create(e.GetType().FullName!, JsonSerializer.Serialize(e, e.GetType()))).ToList();
        context.OutboxMessages.AddRange(messages);
    }
    private async Task DispatchDomainEventsAsync(DbContext context, CancellationToken ct)
    {
        var aggregates = context.ChangeTracker.Entries<IAggregateRoot>().Where(e => e.Entity.DomainEvents.Any()).Select(e => e.Entity).ToList();
        var events = aggregates.SelectMany(a => a.DomainEvents).ToList();
        aggregates.ForEach(a => a.ClearDomainEvents());
        foreach (var e in events) await _mediator.Publish(e, ct);
    }
}