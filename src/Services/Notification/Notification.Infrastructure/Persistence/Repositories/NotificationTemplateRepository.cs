using Microsoft.EntityFrameworkCore;
using Notification.Application.Interfaces;
using Notification.Domain.Entities;
using Notification.Domain.Enums;

namespace Notification.Infrastructure.Persistence.Repositories;

public sealed class NotificationTemplateRepository : INotificationTemplateRepository
{
    private readonly NotificationDbContext _db;
    public NotificationTemplateRepository(NotificationDbContext db) => _db = db;

    public async Task<NotificationTemplate?> GetByEventTypeAsync(Guid tenantId, string eventType, NotificationChannel channel, CancellationToken ct = default) =>
        await _db.NotificationTemplates.FirstOrDefaultAsync(
            t => t.TenantId == tenantId && t.EventType == eventType && t.Channel == channel && t.IsActive, ct);

    public async Task<NotificationTemplate?> GetDefaultAsync(string eventType, NotificationChannel channel, CancellationToken ct = default) =>
        await _db.NotificationTemplates.FirstOrDefaultAsync(
            t => t.TenantId == Guid.Empty && t.EventType == eventType && t.Channel == channel && t.IsActive, ct);

    public async Task AddAsync(NotificationTemplate template, CancellationToken ct = default)
    {
        await _db.NotificationTemplates.AddAsync(template, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(NotificationTemplate template, CancellationToken ct = default)
    {
        _db.NotificationTemplates.Update(template);
        await _db.SaveChangesAsync(ct);
    }
}