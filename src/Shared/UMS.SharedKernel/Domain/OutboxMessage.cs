// UMS — University Management System
// Key:     UMS-SHARED-P0-003
// Service: SharedKernel
// Layer:   Domain
namespace UMS.SharedKernel.Domain;

public sealed class OutboxMessage
{
    public Guid            Id             { get; init; } = Guid.NewGuid();
    public string          EventType      { get; init; } = string.Empty;
    public string          Payload        { get; init; } = string.Empty;
    public string?         TenantId       { get; init; }
    public DateTimeOffset  OccurredAt     { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset  CreatedAt      { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAt    { get; set; }
    public DateTimeOffset? DeadLetteredAt { get; set; }   // was string? — fixed
    public int             RetryCount     { get; set; }
    public string?         Error          { get; set; }

    public static OutboxMessage Create(
        string eventType,
        string payload,
        Guid   tenantId = default) =>
        new()
        {
            EventType = eventType,
            Payload   = payload,
            TenantId  = tenantId == default ? null : tenantId.ToString(),
        };
}
