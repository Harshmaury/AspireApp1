namespace Academic.Domain.Common;
public sealed class OutboxMessage
{
    public Guid Id { get; private set; }
    public string EventType { get; private set; } = default!;
    public string Payload { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public int RetryCount { get; private set; }
    public string? Error { get; private set; }
    private OutboxMessage() { }
    public static OutboxMessage Create(string eventType, string payload) => new()
    {
        Id = Guid.NewGuid(),
        EventType = eventType,
        Payload = payload,
        CreatedAt = DateTime.UtcNow
    };
    public void MarkProcessed() => ProcessedAt = DateTime.UtcNow;
    public void MarkFailed(string error) { Error = error; RetryCount++; }
}