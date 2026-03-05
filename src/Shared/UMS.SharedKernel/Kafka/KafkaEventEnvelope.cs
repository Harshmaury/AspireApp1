namespace UMS.SharedKernel.Kafka;

/// <summary>
/// Standard Kafka event envelope for all UMS domain events.
/// Required for multi-region replication in Phase 3.
/// </summary>
public sealed class KafkaEventEnvelope
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public string EventType { get; init; } = string.Empty;
    public Guid TenantId { get; init; }
    public string RegionOrigin { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public string SchemaVersion { get; init; } = "1.0";
    public string Payload { get; init; } = string.Empty;

    public static KafkaEventEnvelope Create(
        string eventType,
        Guid tenantId,
        string regionOrigin,
        string payload) => new()
    {
        EventType    = eventType,
        TenantId     = tenantId,
        RegionOrigin = regionOrigin,
        Payload      = payload
    };
}