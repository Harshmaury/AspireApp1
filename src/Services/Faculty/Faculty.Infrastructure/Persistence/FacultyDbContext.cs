using Faculty.Domain.Common;
using Faculty.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using UMS.SharedKernel.Tenancy;
using FacultyEntity = Faculty.Domain.Entities.Faculty;

namespace Faculty.Infrastructure.Persistence;

public sealed class FacultyDbContext : DbContext
{
    private readonly ITenantContext? _tenant;

    public FacultyDbContext(
        DbContextOptions<FacultyDbContext> options,
        ITenantContext? tenant = null)
        : base(options)
    {
        _tenant = tenant;
    }

    public DbSet<FacultyEntity>    Faculty           => Set<FacultyEntity>();
    public DbSet<CourseAssignment> CourseAssignments => Set<CourseAssignment>();
    public DbSet<Publication>      Publications      => Set<Publication>();
    public DbSet<OutboxMessage>    OutboxMessages    => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FacultyDbContext).Assembly);
        base.OnModelCreating(modelBuilder);

        // Tenant isolation — filter evaluated per-query via _tenant field reference
        // EF Core evaluates this per DbContext instance, not once at model-build time
        modelBuilder.Entity<FacultyEntity>().HasQueryFilter(
            e => _tenant == null || !_tenant.IsResolved || e.TenantId == _tenant.TenantId);
        modelBuilder.Entity<CourseAssignment>().HasQueryFilter(
            e => _tenant == null || !_tenant.IsResolved || e.TenantId == _tenant.TenantId);
        modelBuilder.Entity<Publication>().HasQueryFilter(
            e => _tenant == null || !_tenant.IsResolved || e.TenantId == _tenant.TenantId);
    }
}
