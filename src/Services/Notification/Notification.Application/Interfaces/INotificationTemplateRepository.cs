using Notification.Domain.Entities;
using Notification.Domain.Enums;
namespace Notification.Application.Interfaces;
public interface INotificationTemplateRepository
{
    Task<NotificationTemplate?> GetByEventTypeAsync(Guid tenantId, string eventType, NotificationChannel channel, CancellationToken ct = default);
    Task<NotificationTemplate?> GetDefaultAsync(string eventType, NotificationChannel channel, CancellationToken ct = default);
    Task AddAsync(NotificationTemplate template, CancellationToken ct = default);
    Task UpdateAsync(NotificationTemplate template, CancellationToken ct = default);
}
