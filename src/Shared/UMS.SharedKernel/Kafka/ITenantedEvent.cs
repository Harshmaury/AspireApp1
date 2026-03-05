namespace UMS.SharedKernel.Kafka;

/// <summary>
/// Marker interface for domain events that carry a TenantId.
/// Required for Kafka envelope construction.
/// </summary>
public interface ITenantedEvent
{
    Guid TenantId { get; }
}