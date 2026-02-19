// src/Services/Identity/Identity.Domain/Common/IAggregateRoot.cs
namespace Identity.Domain.Common;

// Marker interface — only aggregates can raise domain events
public interface IAggregateRoot
{
    IReadOnlyCollection<DomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}