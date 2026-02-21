using Microsoft.EntityFrameworkCore;
using FacultyEntity = Faculty.Domain.Entities.Faculty;
using Faculty.Domain.Entities;
using Faculty.Domain.Common;
namespace Faculty.Infrastructure.Persistence;
public sealed class FacultyDbContext : DbContext
{
    public DbSet<FacultyEntity> Faculty => Set<FacultyEntity>();
    public DbSet<CourseAssignment> CourseAssignments => Set<CourseAssignment>();
    public DbSet<Publication> Publications => Set<Publication>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public FacultyDbContext(DbContextOptions<FacultyDbContext> options) : base(options) { }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FacultyDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
