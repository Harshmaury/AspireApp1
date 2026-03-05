using UMS.SharedKernel.Kafka;

namespace Identity.Domain.Common;

public abstract class DomainEvent : MediatR.INotification, ITenantedEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public abstract Guid TenantId { get; }
}