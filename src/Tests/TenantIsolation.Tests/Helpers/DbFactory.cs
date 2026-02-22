using Microsoft.EntityFrameworkCore;
namespace TenantIsolation.Tests.Helpers;

public static class DbFactory
{
    /// Each call returns a fresh isolated InMemory database.
    /// Pass the same dbName to two calls to share data across context instances.
    public static T Create<T>(Func<DbContextOptions<T>, T> ctor, string? dbName = null)
        where T : DbContext
    {
        var opts = new DbContextOptionsBuilder<T>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;
        return ctor(opts);
    }
}
