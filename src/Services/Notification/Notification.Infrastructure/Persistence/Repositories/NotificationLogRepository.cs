using Microsoft.EntityFrameworkCore;
using Notification.Application.Interfaces;
using Notification.Domain.Entities;
using Notification.Domain.Enums;
namespace Notification.Infrastructure.Persistence.Repositories;
public sealed class NotificationLogRepository : INotificationLogRepository
{
    private readonly NotificationDbContext _context;
    public NotificationLogRepository(NotificationDbContext context) => _context = context;
    public async Task<NotificationLog?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.NotificationLogs.FirstOrDefaultAsync(e => e.Id == id, ct);
    public async Task<List<NotificationLog>> GetFailedAsync(int maxRetry, CancellationToken ct = default) =>
        await _context.NotificationLogs.Where(e => e.Status == NotificationStatus.Failed && e.RetryCount < maxRetry).OrderBy(e => e.CreatedAt).Take(50).ToListAsync(ct);
    public async Task AddAsync(NotificationLog log, CancellationToken ct = default) =>
        await _context.NotificationLogs.AddAsync(log, ct);
    public async Task UpdateAsync(NotificationLog log, CancellationToken ct = default) =>
        await Task.FromResult(_context.NotificationLogs.Update(log));
}
