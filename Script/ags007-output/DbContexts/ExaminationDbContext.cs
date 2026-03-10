using Examination.Domain.Common;
using Examination.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using UMS.SharedKernel.Tenancy;

namespace Examination.Infrastructure.Persistence;

public sealed class ExaminationDbContext : DbContext
{
    private readonly ITenantContext? _tenant;

    public ExaminationDbContext(
        DbContextOptions<ExaminationDbContext> options,
        ITenantContext? tenant = null)
        : base(options)
    {
        _tenant = tenant;
    }

    public DbSet<ExamSchedule>  ExamSchedules  => Set<ExamSchedule>();
    public DbSet<HallTicket>    HallTickets    => Set<HallTicket>();
    public DbSet<MarksEntry>    MarksEntries   => Set<MarksEntry>();
    public DbSet<ResultCard>    ResultCards    => Set<ResultCard>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ExaminationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);

        if (_tenant?.IsResolved == true)
        {
            var tid = _tenant.TenantId;
            modelBuilder.Entity<ExamSchedule>().HasQueryFilter(e => e.TenantId == tid);
            modelBuilder.Entity<HallTicket>()  .HasQueryFilter(e => e.TenantId == tid);
            modelBuilder.Entity<MarksEntry>()  .HasQueryFilter(e => e.TenantId == tid);
            modelBuilder.Entity<ResultCard>()  .HasQueryFilter(e => e.TenantId == tid);
        }
    }
}
