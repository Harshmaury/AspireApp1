using Microsoft.EntityFrameworkCore;
using Notification.Application.Interfaces;
using Notification.Domain.Entities;

namespace Notification.Infrastructure.Persistence.Repositories;

public sealed class NotificationPreferenceRepository : INotificationPreferenceRepository
{
    private readonly NotificationDbContext _db;
    public NotificationPreferenceRepository(NotificationDbContext db) => _db = db;

    public async Task<NotificationPreference?> GetByUserAsync(Guid userId, Guid tenantId, CancellationToken ct = default) =>
        await _db.NotificationPreferences.FirstOrDefaultAsync(e => e.UserId == userId && e.TenantId == tenantId, ct);

    public async Task AddAsync(NotificationPreference preference, CancellationToken ct = default)
    {
        await _db.NotificationPreferences.AddAsync(preference, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(NotificationPreference preference, CancellationToken ct = default)
    {
        _db.NotificationPreferences.Update(preference);
        await _db.SaveChangesAsync(ct);
    }
}