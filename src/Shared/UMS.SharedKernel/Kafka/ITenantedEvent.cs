// ============================================================
// UMS.SharedKernel — ITenantedEvent
// Marker interface for domain events carrying a TenantId.
// Required by DomainEventDispatcherInterceptorBase for envelope construction.
// ============================================================
namespace UMS.SharedKernel.Kafka;

/// <summary>
/// Marker interface for domain events that must carry a TenantId.
/// Any domain event routed through the Outbox MUST implement this.
/// </summary>
public interface ITenantedEvent
{
    Guid TenantId { get; }
}
