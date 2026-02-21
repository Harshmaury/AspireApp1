using Microsoft.EntityFrameworkCore;
using Notification.Application.Interfaces;
using Notification.Domain.Entities;
using Notification.Domain.Enums;
namespace Notification.Infrastructure.Persistence.Repositories;
public sealed class NotificationTemplateRepository : INotificationTemplateRepository
{
    private readonly NotificationDbContext _context;
    public NotificationTemplateRepository(NotificationDbContext context) => _context = context;
    public async Task<NotificationTemplate?> GetByEventTypeAsync(Guid tenantId, string eventType, NotificationChannel channel, CancellationToken ct = default) =>
        await _context.NotificationTemplates.FirstOrDefaultAsync(t => t.TenantId == tenantId && t.EventType == eventType && t.Channel == channel && t.IsActive, ct);
    public async Task<NotificationTemplate?> GetDefaultAsync(string eventType, NotificationChannel channel, CancellationToken ct = default) =>
        await _context.NotificationTemplates.FirstOrDefaultAsync(t => t.TenantId == Guid.Empty && t.EventType == eventType && t.Channel == channel && t.IsActive, ct);
    public async Task AddAsync(NotificationTemplate template, CancellationToken ct = default) =>
        await _context.NotificationTemplates.AddAsync(template, ct);
    public async Task UpdateAsync(NotificationTemplate template, CancellationToken ct = default) =>
        await Task.FromResult(_context.NotificationTemplates.Update(template));
}
