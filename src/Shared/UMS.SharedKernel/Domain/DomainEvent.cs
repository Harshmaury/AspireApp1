// UMS â€” University Management System
// Key:     UMS-SHARED-P0-002
// Service: SharedKernel
// Layer:   Domain
using UMS.SharedKernel.Kafka;

namespace UMS.SharedKernel.Domain;

public abstract class DomainEvent : IDomainEvent, ITenantedEvent
{
    public Guid           EventId    { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
    public abstract Guid  TenantId   { get; }
}
