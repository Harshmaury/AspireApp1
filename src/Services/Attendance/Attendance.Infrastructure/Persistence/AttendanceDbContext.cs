using UMS.SharedKernel.Domain;
using Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using UMS.SharedKernel.Tenancy;

namespace Attendance.Infrastructure.Persistence;

public sealed class AttendanceDbContext : DbContext
{
    private readonly ITenantContext? _tenant;

    public AttendanceDbContext(
        DbContextOptions<AttendanceDbContext> options,
        ITenantContext? tenant = null)
        : base(options)
    {
        _tenant = tenant;
    }

    public DbSet<AttendanceRecord>   AttendanceRecords   => Set<AttendanceRecord>();
    public DbSet<AttendanceSummary>  AttendanceSummaries => Set<AttendanceSummary>();
    public DbSet<CondonationRequest> CondonationRequests => Set<CondonationRequest>();
    public DbSet<OutboxMessage>      OutboxMessages      => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AttendanceDbContext).Assembly);
        base.OnModelCreating(modelBuilder);

        // Tenant isolation — filter evaluated per-query via _tenant field reference
        // EF Core evaluates this per DbContext instance, not once at model-build time
        modelBuilder.Entity<AttendanceRecord>().HasQueryFilter(
            e => _tenant == null || !_tenant.IsResolved || e.TenantId == _tenant.TenantId);
        modelBuilder.Entity<AttendanceSummary>().HasQueryFilter(
            e => _tenant == null || !_tenant.IsResolved || e.TenantId == _tenant.TenantId);
        modelBuilder.Entity<CondonationRequest>().HasQueryFilter(
            e => _tenant == null || !_tenant.IsResolved || e.TenantId == _tenant.TenantId);
    }
}
