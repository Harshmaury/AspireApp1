using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace Fee.Infrastructure.Persistence;
public sealed class FeeDbContextFactory : IDesignTimeDbContextFactory<FeeDbContext>
{
    public FeeDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<FeeDbContext>()
            .UseNpgsql("Host=localhost;Database=FeeDb;Username=postgres;Password=postgres")
            .Options;
        return new FeeDbContext(options);
    }
}
