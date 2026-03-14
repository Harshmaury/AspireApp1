namespace UMS.SharedKernel.Kafka;

public sealed class KafkaEventEnvelope
{
    public Guid           EventId       { get; init; } = Guid.NewGuid();
    public string         EventType     { get; init; } = string.Empty;
    public string         TenantId      { get; init; } = string.Empty;
    public string         RegionOrigin  { get; init; } = string.Empty;
    public DateTimeOffset OccurredAt    { get; init; } = DateTimeOffset.UtcNow;
    public string         SchemaVersion { get; init; } = "1.0";
    public string         Payload       { get; init; } = string.Empty;

    public static KafkaEventEnvelope Create(
        string eventType,
        Guid   tenantId,
        string regionOrigin,
        string payload) => new()
    {
        EventType    = eventType,
        TenantId     = tenantId.ToString(),
        RegionOrigin = regionOrigin,
        Payload      = payload
    };
}
