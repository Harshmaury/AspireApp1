using Microsoft.EntityFrameworkCore;
using Attendance.Domain.Entities;
using Attendance.Domain.Common;
namespace Attendance.Infrastructure.Persistence;
public sealed class AttendanceDbContext : DbContext
{
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<AttendanceSummary> AttendanceSummaries => Set<AttendanceSummary>();
    public DbSet<CondonationRequest> CondonationRequests => Set<CondonationRequest>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public AttendanceDbContext(DbContextOptions<AttendanceDbContext> options) : base(options) { }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AttendanceDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
