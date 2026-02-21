using Notification.Domain.Entities;
namespace Notification.Application.Interfaces;
public interface INotificationPreferenceRepository
{
    Task<NotificationPreference?> GetByUserAsync(Guid userId, Guid tenantId, CancellationToken ct = default);
    Task AddAsync(NotificationPreference preference, CancellationToken ct = default);
    Task UpdateAsync(NotificationPreference preference, CancellationToken ct = default);
}
