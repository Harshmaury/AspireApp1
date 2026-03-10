using Fee.Domain.Common;
using Fee.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using UMS.SharedKernel.Tenancy;

namespace Fee.Infrastructure.Persistence;

public sealed class FeeDbContext : DbContext
{
    private readonly ITenantContext? _tenant;

    public FeeDbContext(
        DbContextOptions<FeeDbContext> options,
        ITenantContext? tenant = null)
        : base(options)
    {
        _tenant = tenant;
    }

    public DbSet<FeeStructure>  FeeStructures  => Set<FeeStructure>();
    public DbSet<FeePayment>    FeePayments    => Set<FeePayment>();
    public DbSet<Scholarship>   Scholarships   => Set<Scholarship>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FeeDbContext).Assembly);
        base.OnModelCreating(modelBuilder);

        if (_tenant?.IsResolved == true)
        {
            var tid = _tenant.TenantId;
            modelBuilder.Entity<FeeStructure>().HasQueryFilter(e => e.TenantId == tid);
            modelBuilder.Entity<FeePayment>()  .HasQueryFilter(e => e.TenantId == tid);
            modelBuilder.Entity<Scholarship>() .HasQueryFilter(e => e.TenantId == tid);
        }
    }
}
