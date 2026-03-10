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

        // Tenant isolation — filter evaluated per-query via _tenant field reference
        // EF Core evaluates this per DbContext instance, not once at model-build time
        modelBuilder.Entity<NotificationTemplate>().HasQueryFilter(
            e => _tenant == null || !_tenant.IsResolved || e.TenantId == _tenant.TenantId);
        modelBuilder.Entity<NotificationLog>().HasQueryFilter(
            e => _tenant == null || !_tenant.IsResolved || e.TenantId == _tenant.TenantId);
        modelBuilder.Entity<NotificationPreference>().HasQueryFilter(
            e => _tenant == null || !_tenant.IsResolved || e.TenantId == _tenant.TenantId);
    }
}
