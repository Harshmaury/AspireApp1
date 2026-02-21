using Hostel.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
namespace Hostel.Infrastructure.Persistence;
public sealed class DomainEventDispatcherInterceptor(IMediator mediator) : SaveChangesInterceptor
{
    public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData e, int result, CancellationToken ct = default)
    {
        if (e.Context is not null)
        {
            var aggregates = e.Context.ChangeTracker.Entries<AggregateRoot>()
                .Select(x => x.Entity)
                .Where(x => x.DomainEvents.Any())
                .ToList();
            var events = aggregates.SelectMany(x => x.DomainEvents).ToList();
            aggregates.ForEach(x => x.ClearDomainEvents());
            foreach (var domainEvent in events)
                await mediator.Publish(domainEvent, ct);
        }
        return await base.SavedChangesAsync(e, result, ct);
    }
}
