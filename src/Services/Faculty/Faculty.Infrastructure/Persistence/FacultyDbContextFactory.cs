using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace Faculty.Infrastructure.Persistence;
public sealed class FacultyDbContextFactory : IDesignTimeDbContextFactory<FacultyDbContext>
{
    public FacultyDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<FacultyDbContext>()
            .UseNpgsql("Host=localhost;Database=FacultyDb;Username=postgres;Password=postgres")
            .Options;
        return new FacultyDbContext(options);
    }
}
