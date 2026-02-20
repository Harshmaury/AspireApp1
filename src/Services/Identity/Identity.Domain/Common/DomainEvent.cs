namespace Identity.Domain.Common;

public abstract class DomainEvent : MediatR.INotification
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
