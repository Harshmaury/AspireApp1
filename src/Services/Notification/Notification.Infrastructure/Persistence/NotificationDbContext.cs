using Microsoft.EntityFrameworkCore;
using Notification.Domain.Entities;
namespace Notification.Infrastructure.Persistence;
public sealed class NotificationDbContext : DbContext
{
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
