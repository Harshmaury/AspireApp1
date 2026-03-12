// UMS â€” University Management System
// Key:     UMS-SHARED-P0-002
// Service: SharedKernel
// Layer:   Domain
namespace UMS.SharedKernel.Domain;

public interface IAggregateRoot
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
