using Notification.Domain.Common;
using Notification.Domain.Enums;
using Notification.Domain.Exceptions;
namespace Notification.Domain.Entities;
public sealed class NotificationLog : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid RecipientId { get; private set; }
    public string RecipientAddress { get; private set; } = default!;
    public string EventType { get; private set; } = default!;
    public NotificationChannel Channel { get; private set; }
    public string Subject { get; private set; } = default!;
    public string Body { get; private set; } = default!;
    public NotificationStatus Status { get; private set; }
    public int RetryCount { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? SentAt { get; private set; }
    private NotificationLog() { }
    public static NotificationLog Create(Guid tenantId, Guid recipientId, string recipientAddress, string eventType, NotificationChannel channel, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(recipientAddress)) throw new NotificationDomainException("INVALID_ADDRESS", "Recipient address is required.");
        if (string.IsNullOrWhiteSpace(subject)) throw new NotificationDomainException("INVALID_SUBJECT", "Subject is required.");
        return new NotificationLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RecipientId = recipientId,
            RecipientAddress = recipientAddress.Trim(),
            EventType = eventType.Trim(),
            Channel = channel,
            Subject = subject.Trim(),
            Body = body.Trim(),
            Status = NotificationStatus.Pending,
            RetryCount = 0,
            CreatedAt = DateTime.UtcNow
        };
    }
    public void MarkSent()
    {
        Status = NotificationStatus.Sent;
        SentAt = DateTime.UtcNow;
    }
    public void MarkFailed(string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage)) throw new NotificationDomainException("INVALID_ERROR", "Error message is required.");
        RetryCount++;
        ErrorMessage = errorMessage;
        Status = RetryCount >= 5 ? NotificationStatus.DeadLettered : NotificationStatus.Failed;
    }
    public bool CanRetry() => Status == NotificationStatus.Failed && RetryCount < 5;
}
