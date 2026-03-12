// UMS â€” University Management System
// Key:     UMS-SHARED-P0-002
// Service: SharedKernel
// Layer:   Domain
namespace UMS.SharedKernel.Domain;

public abstract class AggregateRoot : BaseEntity, IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
        => _domainEvents.Add(domainEvent);
}
