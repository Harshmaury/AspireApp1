namespace Fee.Infrastructure.Outbox;

public sealed class OutboxMessage
{
    public Guid      Id             { get; init; } = Guid.NewGuid();
    public string    Type           { get; init; } = string.Empty;
    public string    Payload        { get; init; } = string.Empty;
    public Guid      TenantId       { get; init; }
    public DateTime  OccurredAt     { get; init; } = DateTime.UtcNow;
    public DateTime? ProcessedAt    { get; set; }
    public int       RetryCount     { get; set; }
    public DateTime? DeadLetteredAt { get; set; }
    public string?   Error          { get; set; }

    public static OutboxMessage Create(string type, string payload, Guid tenantId = default) =>
        new() { Type = type, Payload = payload, TenantId = tenantId };
}
