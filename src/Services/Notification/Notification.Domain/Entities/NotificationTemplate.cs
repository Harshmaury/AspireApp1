using Notification.Domain.Common;
using Notification.Domain.Enums;
using Notification.Domain.Exceptions;
namespace Notification.Domain.Entities;
public sealed class NotificationTemplate : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string EventType { get; private set; } = default!;
    public NotificationChannel Channel { get; private set; }
    public string SubjectTemplate { get; private set; } = default!;
    public string BodyTemplate { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    private NotificationTemplate() { }
    public static NotificationTemplate Create(Guid tenantId, string eventType, NotificationChannel channel, string subjectTemplate, string bodyTemplate)
    {
        if (string.IsNullOrWhiteSpace(eventType)) throw new NotificationDomainException("INVALID_EVENT_TYPE", "Event type is required.");
        if (string.IsNullOrWhiteSpace(subjectTemplate)) throw new NotificationDomainException("INVALID_SUBJECT", "Subject template is required.");
        if (string.IsNullOrWhiteSpace(bodyTemplate)) throw new NotificationDomainException("INVALID_BODY", "Body template is required.");
        return new NotificationTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EventType = eventType.Trim(),
            Channel = channel,
            SubjectTemplate = subjectTemplate.Trim(),
            BodyTemplate = bodyTemplate.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }
    public void Update(string subjectTemplate, string bodyTemplate)
    {
        if (string.IsNullOrWhiteSpace(subjectTemplate)) throw new NotificationDomainException("INVALID_SUBJECT", "Subject template is required.");
        if (string.IsNullOrWhiteSpace(bodyTemplate)) throw new NotificationDomainException("INVALID_BODY", "Body template is required.");
        SubjectTemplate = subjectTemplate.Trim();
        BodyTemplate = bodyTemplate.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
    public void Deactivate()
    {
        if (!IsActive) throw new NotificationDomainException("ALREADY_INACTIVE", "Template is already inactive.");
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
    public string RenderSubject(Dictionary<string, string> data) => Render(SubjectTemplate, data);
    public string RenderBody(Dictionary<string, string> data) => Render(BodyTemplate, data);
    private static string Render(string template, Dictionary<string, string> data) =>
        data.Aggregate(template, (current, kv) => current.Replace($"{{{{{kv.Key}}}}}", kv.Value));
}
