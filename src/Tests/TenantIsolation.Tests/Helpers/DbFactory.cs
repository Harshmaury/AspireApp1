using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace TenantIsolation.Tests.Helpers;

// TST-4 fix: switched from InMemory to real Postgres.
// Each unique schemaName (a Guid string) maps to a dedicated Postgres schema,
// giving the same write-then-read isolation the InMemory tests relied on,
// but now on a provider that faithfully enforces EF global query filters.
public static class DbFactory
{
    public static T Create<T>(
        Func<DbContextOptions<T>, T> ctor,
        string connectionString,
        string schemaName)
        where T : DbContext
    {
        // Strip hyphens — Postgres schema names cannot contain them unquoted
        var safeSchema = "t" + schemaName.Replace("-", "");

        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            SearchPath = safeSchema
        };

        var opts = new DbContextOptionsBuilder<T>()
            .UseNpgsql(builder.ConnectionString)
            .Options;

        var ctx = ctor(opts);

        // Create the schema and tables if not already present for this test
        ctx.Database.ExecuteSqlRaw($"CREATE SCHEMA IF NOT EXISTS \"{safeSchema}\"");
        ctx.Database.EnsureCreated();

        return ctx;
    }
}
