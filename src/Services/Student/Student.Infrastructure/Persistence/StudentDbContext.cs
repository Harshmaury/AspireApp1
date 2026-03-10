using Microsoft.EntityFrameworkCore;
using Student.Domain.Common;
using Student.Domain.Entities;
using UMS.SharedKernel.Tenancy;

namespace Student.Infrastructure.Persistence;

public sealed class StudentDbContext : DbContext
{
    private readonly ITenantContext? _tenant;

    public StudentDbContext(
        DbContextOptions<StudentDbContext> options,
        ITenantContext? tenant = null)
        : base(options)
    {
        _tenant = tenant;
    }

    public DbSet<StudentAggregate> Students       => Set<StudentAggregate>();
    public DbSet<OutboxMessage>    OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StudentDbContext).Assembly);
        base.OnModelCreating(modelBuilder);

        // Tenant isolation — filter evaluated per-query via _tenant field reference
        // EF Core evaluates this per DbContext instance, not once at model-build time
        modelBuilder.Entity<StudentAggregate>().HasQueryFilter(
            e => _tenant == null || !_tenant.IsResolved || e.TenantId == _tenant.TenantId);
    }
}
