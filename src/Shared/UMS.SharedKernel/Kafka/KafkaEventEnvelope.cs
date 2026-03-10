// UMS.SharedKernel/Kafka/KafkaEventEnvelope.cs
// Standard wire format for all UMS domain events on Kafka.
namespace UMS.SharedKernel.Kafka;

/// <summary>
/// Standard Kafka event envelope for all UMS domain events.
/// All producers wrap their payload in this envelope.
/// All consumers deserialize this envelope first, then route by EventType.
/// </summary>
public sealed class KafkaEventEnvelope
{
    public Guid     EventId       { get; init; } = Guid.NewGuid();
    public string   EventType     { get; init; } = string.Empty;
    public string   TenantId      { get; init; } = string.Empty;
    public string   RegionOrigin  { get; init; } = string.Empty;
    public DateTime OccurredAt    { get; init; } = DateTime.UtcNow;
    public string   SchemaVersion { get; init; } = "1.0";
    public string   Payload       { get; init; } = string.Empty;

    public static KafkaEventEnvelope Create(
        string eventType,
        Guid   tenantId,
        string regionOrigin,
        string payload) => new()
    {
        EventType    = eventType,
        TenantId     = tenantId.ToString(),   // Guid -> string
        RegionOrigin = regionOrigin,
        Payload      = payload
    };
}
