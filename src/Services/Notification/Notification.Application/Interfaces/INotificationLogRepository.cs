using Notification.Domain.Entities;
using Notification.Domain.Enums;
namespace Notification.Application.Interfaces;
public interface INotificationLogRepository
{
    Task<NotificationLog?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<NotificationLog>> GetFailedAsync(int maxRetry, CancellationToken ct = default);
    Task AddAsync(NotificationLog log, CancellationToken ct = default);
    Task UpdateAsync(NotificationLog log, CancellationToken ct = default);
}
