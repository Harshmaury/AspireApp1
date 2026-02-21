using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace Academic.Infrastructure.Persistence;
internal sealed class AcademicDbContextFactory : IDesignTimeDbContextFactory<AcademicDbContext>
{
    public AcademicDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AcademicDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=AcademicDb;Username=postgres;Password=postgres")
            .Options;
        return new AcademicDbContext(options);
    }
}