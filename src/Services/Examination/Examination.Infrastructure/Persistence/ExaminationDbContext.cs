using UMS.SharedKernel.Domain;
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

        // Tenant isolation — filter evaluated per-query via _tenant field reference
        // EF Core evaluates this per DbContext instance, not once at model-build time
        modelBuilder.Entity<ExamSchedule>().HasQueryFilter(
            e => _tenant == null || !_tenant.IsResolved || e.TenantId == _tenant.TenantId);
        modelBuilder.Entity<HallTicket>().HasQueryFilter(
            e => _tenant == null || !_tenant.IsResolved || e.TenantId == _tenant.TenantId);
        modelBuilder.Entity<MarksEntry>().HasQueryFilter(
            e => _tenant == null || !_tenant.IsResolved || e.TenantId == _tenant.TenantId);
        modelBuilder.Entity<ResultCard>().HasQueryFilter(
            e => _tenant == null || !_tenant.IsResolved || e.TenantId == _tenant.TenantId);
    }
}
