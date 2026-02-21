using MediatR;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Fee.Domain.Common;
namespace Fee.Infrastructure.Persistence;
public sealed class DomainEventDispatcherInterceptor : SaveChangesInterceptor
{
    private readonly IMediator _mediator;
    public DomainEventDispatcherInterceptor(IMediator mediator) => _mediator = mediator;
    public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken ct = default)
    {
        if (eventData.Context is not null)
        {
            var aggregates = eventData.Context.ChangeTracker.Entries<AggregateRoot>()
                .Select(e => e.Entity)
                .Where(e => e.DomainEvents.Any())
                .ToList();
            foreach (var aggregate in aggregates)
            {
                foreach (var domainEvent in aggregate.DomainEvents)
                    await _mediator.Publish(domainEvent, ct);
                aggregate.ClearDomainEvents();
            }
        }
        return result;
    }
}
