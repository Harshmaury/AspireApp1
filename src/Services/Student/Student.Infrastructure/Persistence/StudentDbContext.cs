using Microsoft.EntityFrameworkCore;
using Student.Domain.Common;
using Student.Domain.Entities;

namespace Student.Infrastructure.Persistence;

public sealed class StudentDbContext : DbContext
{
    public StudentDbContext(DbContextOptions<StudentDbContext> options) : base(options) { }

    public DbSet<StudentAggregate> Students => Set<StudentAggregate>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StudentDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

