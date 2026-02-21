using Microsoft.EntityFrameworkCore;
using Notification.Application.Interfaces;
using Notification.Domain.Entities;
namespace Notification.Infrastructure.Persistence.Repositories;
public sealed class NotificationPreferenceRepository : INotificationPreferenceRepository
{
    private readonly NotificationDbContext _context;
    public NotificationPreferenceRepository(NotificationDbContext context) => _context = context;
    public async Task<NotificationPreference?> GetByUserAsync(Guid userId, Guid tenantId, CancellationToken ct = default) =>
        await _context.NotificationPreferences.FirstOrDefaultAsync(e => e.UserId == userId && e.TenantId == tenantId, ct);
    public async Task AddAsync(NotificationPreference preference, CancellationToken ct = default) =>
        await _context.NotificationPreferences.AddAsync(preference, ct);
    public async Task UpdateAsync(NotificationPreference preference, CancellationToken ct = default) =>
        await Task.FromResult(_context.NotificationPreferences.Update(preference));
}
