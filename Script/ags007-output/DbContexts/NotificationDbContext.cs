using Microsoft.EntityFrameworkCore;
using Notification.Domain.Entities;
using UMS.SharedKernel.Tenancy;

namespace Notification.Infrastructure.Persistence;

public sealed class NotificationDbContext : DbContext
{
    private readonly ITenantContext? _tenant;

    public NotificationDbContext(
        DbContextOptions<NotificationDbContext> options,
        ITenantContext? tenant = null)
        : base(options)
    {
        _tenant = tenant;
    }

    public DbSet<NotificationTemplate>   NotificationTemplates   => Set<NotificationTemplate>();
    public DbSet<NotificationLog>        NotificationLogs        => Set<NotificationLog>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);

        if (_tenant?.IsResolved == true)
        {
            var tid = _tenant.TenantId;
            modelBuilder.Entity<NotificationTemplate>()  .HasQueryFilter(e => e.TenantId == tid);
            modelBuilder.Entity<NotificationLog>()       .HasQueryFilter(e => e.TenantId == tid);
            modelBuilder.Entity<NotificationPreference>().HasQueryFilter(e => e.TenantId == tid);
        }
    }
}
