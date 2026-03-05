using MediatR;
using UMS.SharedKernel.Kafka;

namespace Student.Domain.Common;

public abstract class DomainEvent : INotification, ITenantedEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public abstract Guid TenantId { get; }
}