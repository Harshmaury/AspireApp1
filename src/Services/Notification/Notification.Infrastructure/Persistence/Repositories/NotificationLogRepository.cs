using Microsoft.EntityFrameworkCore;
using Notification.Application.Interfaces;
using Notification.Domain.Entities;
using Notification.Domain.Enums;
using UMS.SharedKernel.Tenancy;

namespace Notification.Infrastructure.Persistence.Repositories;

internal sealed class NotificationLogRepository : INotificationLogRepository
{
    private readonly NotificationDbContext _db;
    public NotificationLogRepository(NotificationDbContext db, ITenantContext? tenant = null) => _db = db;

    public async Task<NotificationLog?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.NotificationLogs.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<List<NotificationLog>> GetByRecipientAsync(Guid recipientId, Guid tenantId, int page, int pageSize, CancellationToken ct = default) =>
        await _db.NotificationLogs
            .Where(e => e.RecipientId == recipientId && e.TenantId == tenantId)
            .OrderByDescending(e => e.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

    public async Task<List<NotificationLog>> GetFailedAsync(int maxRetry, CancellationToken ct = default) =>
        await _db.NotificationLogs
            .Where(e => e.Status == NotificationStatus.Failed && e.RetryCount < maxRetry)
            .OrderBy(e => e.CreatedAt).Take(50).ToListAsync(ct);

    public async Task AddAsync(NotificationLog log, CancellationToken ct = default)
    {
        await _db.NotificationLogs.AddAsync(log, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(NotificationLog log, CancellationToken ct = default)
    {
        if (_db.Entry(log).State == EntityState.Detached)
            throw new InvalidOperationException(
                $"UpdateAsync received a detached NotificationLog (Id={log.Id}). " +
                "Fetch the entity via the repository in the same handler scope before mutating it.");
        await _db.SaveChangesAsync(ct);
    }
}

