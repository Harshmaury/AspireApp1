using Attendance.Domain.Common;
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

        if (_tenant?.IsResolved == true)
        {
            var tid = _tenant.TenantId;
            modelBuilder.Entity<AttendanceRecord>()  .HasQueryFilter(e => e.TenantId == tid);
            modelBuilder.Entity<AttendanceSummary>() .HasQueryFilter(e => e.TenantId == tid);
            modelBuilder.Entity<CondonationRequest>().HasQueryFilter(e => e.TenantId == tid);
        }
    }
}
