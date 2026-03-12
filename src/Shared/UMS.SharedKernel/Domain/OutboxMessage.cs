// UMS — University Management System
// Key: UMS-SHARED-P0-002
// Layer: Shared / Domain
// ─────────────────────────────────────────────────────────────
// CHANGES from original:
//   • TenantId changed from string? to Guid? — type-safe, matches BaseEntity
//   • RegionOrigin added — needed by OutboxRelayServiceBase for KafkaEventEnvelope
//   • CorrelationId added — for distributed tracing across services
// ─────────────────────────────────────────────────────────────
namespace UMS.SharedKernel.Domain;

/// <summary>
/// Canonical outbox message persisted alongside aggregate state changes
/// in the same DB transaction. Relayed to Kafka by
/// <see cref="UMS.SharedKernel.Infrastructure.OutboxRelayServiceBase{TDbContext}"/>.
/// <para>
/// Written by <see cref="UMS.SharedKernel.Infrastructure.DomainEventDispatcherInterceptorBase"/>
/// after EF Core intercepts SaveChanges.
/// </para>
/// </summary>
public sealed class OutboxMessage
{
    /// <summary>Unique message identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Fully qualified event type name, e.g. "StudentCreatedEvent".</summary>
    public string EventType { get; init; } = string.Empty;

    /// <summary>JSON-serialised event payload.</summary>
    public string Payload { get; init; } = string.Empty;

    /// <summary>
    /// Tenant that owns the aggregate that raised this event.
    /// Guid? — nullable because system-level events (e.g. seed) may have no tenant.
    /// </summary>
    public Guid? TenantId { get; init; }

    /// <summary>
    /// Region where the event originated.
    /// Injected from REGION_ID environment variable at relay time.
    /// </summary>
    public string? RegionOrigin { get; init; }

    /// <summary>Optional correlation/trace ID for distributed tracing.</summary>
    public string? CorrelationId { get; init; }

    /// <summary>UTC time the domain event was raised.</summary>
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Set by the relay service after successful Kafka publish.
    /// Null = pending. Non-null = processed.
    /// Uses 'set' (not 'init') — relay must assign after publish.
    /// </summary>
    public DateTimeOffset? ProcessedAt { get; set; }
}
