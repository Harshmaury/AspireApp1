using Microsoft.EntityFrameworkCore;
using Notification.Application.Interfaces;
using Notification.Domain.Entities;
using UMS.SharedKernel.Tenancy;

namespace Notification.Infrastructure.Persistence.Repositories;

internal sealed class NotificationPreferenceRepository : INotificationPreferenceRepository
{
    private readonly NotificationDbContext _db;
    public NotificationPreferenceRepository(NotificationDbContext db, ITenantContext? tenant = null) => _db = db;

    public async Task<NotificationPreference?> GetByUserAsync(Guid userId, Guid tenantId, CancellationToken ct = default) =>
        await _db.NotificationPreferences.FirstOrDefaultAsync(e => e.UserId == userId && e.TenantId == tenantId, ct);

    public async Task AddAsync(NotificationPreference preference, CancellationToken ct = default)
    {
        await _db.NotificationPreferences.AddAsync(preference, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(NotificationPreference preference, CancellationToken ct = default)
    {
        if (_db.Entry(preference).State == EntityState.Detached)
            throw new InvalidOperationException(
                $"UpdateAsync received a detached NotificationPreference (Id={preference.Id}). " +
                "Fetch the entity via the repository in the same handler scope before mutating it.");
        await _db.SaveChangesAsync(ct);
    }
}

