using Notification.Domain.Common;
using Notification.Domain.Exceptions;
namespace Notification.Domain.Entities;
public sealed class NotificationPreference : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public bool EmailEnabled { get; private set; }
    public bool SmsEnabled { get; private set; }
    public bool PushEnabled { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    private NotificationPreference() { }
    public static NotificationPreference Create(Guid tenantId, Guid userId, bool emailEnabled = true, bool smsEnabled = true, bool pushEnabled = false)
    {
        if (tenantId == Guid.Empty) throw new NotificationDomainException("INVALID_TENANT", "TenantId is required.");
        if (userId == Guid.Empty) throw new NotificationDomainException("INVALID_USER", "UserId is required.");
        return new NotificationPreference
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            EmailEnabled = emailEnabled,
            SmsEnabled = smsEnabled,
            PushEnabled = pushEnabled,
            CreatedAt = DateTime.UtcNow
        };
    }
    public void Update(bool emailEnabled, bool smsEnabled, bool pushEnabled)
    {
        EmailEnabled = emailEnabled;
        SmsEnabled = smsEnabled;
        PushEnabled = pushEnabled;
        UpdatedAt = DateTime.UtcNow;
    }
}
