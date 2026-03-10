using Academic.Domain.Common;
using Academic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using UMS.SharedKernel.Tenancy;

namespace Academic.Infrastructure.Persistence;

public sealed class AcademicDbContext : DbContext
{
    private readonly ITenantContext? _tenant;

    public AcademicDbContext(
        DbContextOptions<AcademicDbContext> options,
        ITenantContext? tenant = null)
        : base(options)
    {
        _tenant = tenant;
    }

    public DbSet<Department>       Departments       => Set<Department>();
    public DbSet<Programme>        Programmes        => Set<Programme>();
    public DbSet<Course>           Courses           => Set<Course>();
    public DbSet<Curriculum>       Curricula         => Set<Curriculum>();
    public DbSet<AcademicCalendar> AcademicCalendars => Set<AcademicCalendar>();
    public DbSet<OutboxMessage>    OutboxMessages    => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AcademicDbContext).Assembly);
        base.OnModelCreating(modelBuilder);

        if (_tenant?.IsResolved == true)
        {
            var tid = _tenant.TenantId;
            modelBuilder.Entity<Department>()      .HasQueryFilter(e => e.TenantId == tid);
            modelBuilder.Entity<Programme>()       .HasQueryFilter(e => e.TenantId == tid);
            modelBuilder.Entity<Course>()          .HasQueryFilter(e => e.TenantId == tid);
            modelBuilder.Entity<Curriculum>()      .HasQueryFilter(e => e.TenantId == tid);
            modelBuilder.Entity<AcademicCalendar>().HasQueryFilter(e => e.TenantId == tid);
        }
    }
}
