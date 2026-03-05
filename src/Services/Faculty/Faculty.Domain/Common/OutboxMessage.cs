namespace Faculty.Domain.Common;

public sealed class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    public string? Error { get; set; }
    public const int MaxRetries = 5;
    public bool IsProcessed => ProcessedAt.HasValue;

    private OutboxMessage() { }

    public static OutboxMessage Create(string eventType, string payload) => new()
    {
        Id        = Guid.NewGuid(),
        EventType = eventType,
        Payload   = payload,
        CreatedAt = DateTime.UtcNow
    };

    public void MarkProcessed() => ProcessedAt = DateTime.UtcNow;

    public void MarkFailed(string error)
    {
        RetryCount++;
        Error = error;
    }
}