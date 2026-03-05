namespace Examination.Domain.Common;

public sealed class OutboxMessage
{
    public Guid      Id          { get; set; } = Guid.NewGuid();
    public string    EventType   { get; set; } = default!;
    public string    Payload     { get; set; } = default!;
    public DateTime  CreatedAt   { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public int       RetryCount  { get; set; }
    public string?   Error       { get; set; }

    public void MarkProcessed()
    {
        ProcessedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string error)
    {
        RetryCount++;
        Error = error;
    }
}