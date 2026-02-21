using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace Hostel.Infrastructure.Persistence;
public sealed class HostelDbContextFactory : IDesignTimeDbContextFactory<HostelDbContext>
{
    public HostelDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<HostelDbContext>()
            .UseNpgsql("Host=localhost;Database=HostelDb;Username=postgres;Password=postgres")
            .Options;
        return new HostelDbContext(opts);
    }
}
