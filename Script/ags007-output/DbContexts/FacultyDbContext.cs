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

        if (_tenant?.IsResolved == true)
        {
            var tid = _tenant.TenantId;
            modelBuilder.Entity<FacultyEntity>()   .HasQueryFilter(e => e.TenantId == tid);
            modelBuilder.Entity<CourseAssignment>().HasQueryFilter(e => e.TenantId == tid);
            modelBuilder.Entity<Publication>()     .HasQueryFilter(e => e.TenantId == tid);
        }
    }
}
