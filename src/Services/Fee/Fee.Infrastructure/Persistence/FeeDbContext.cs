using Microsoft.EntityFrameworkCore;
using Fee.Domain.Entities;
using Fee.Domain.Common;
namespace Fee.Infrastructure.Persistence;
public sealed class FeeDbContext : DbContext
{
    public DbSet<FeeStructure> FeeStructures => Set<FeeStructure>();
    public DbSet<FeePayment> FeePayments => Set<FeePayment>();
    public DbSet<Scholarship> Scholarships => Set<Scholarship>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public FeeDbContext(DbContextOptions<FeeDbContext> options) : base(options) { }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FeeDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
