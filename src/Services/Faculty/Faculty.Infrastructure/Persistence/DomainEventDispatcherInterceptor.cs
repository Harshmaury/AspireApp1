using MediatR;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Faculty.Domain.Common;
namespace Faculty.Infrastructure.Persistence;
public sealed class DomainEventDispatcherInterceptor : SaveChangesInterceptor
{
    private readonly IMediator _mediator;
    public DomainEventDispatcherInterceptor(IMediator mediator) => _mediator = mediator;
    public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken ct = default)
    {
        if (eventData.Context is not null)
        {
            var aggregates = eventData.Context.ChangeTracker.Entries<AggregateRoot>()
                .Select(e => e.Entity).Where(e => e.DomainEvents.Any()).ToList();
            foreach (var aggregate in aggregates)
            {
                foreach (var domainEvent in aggregate.DomainEvents)
                {
                    try { await _mediator.Publish(domainEvent, ct); }
                    catch { /* log and continue — do not rethrow */ }
                }
                aggregate.ClearDomainEvents();
            }
        }
        return result;
    }
}
