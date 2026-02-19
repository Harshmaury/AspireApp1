using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Identity.Infrastructure.Persistence;

// Only used by EF Core tooling at design time (migrations)
// Never runs in production
internal sealed class ApplicationDbContextFactory
    : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=IdentityDb;Username=postgres;Password=postgres")
            .UseOpenIddict()
            .Options;

        return new ApplicationDbContext(options);
    }
}
