using Microsoft.EntityFrameworkCore;
using Npgsql;
using UMS.SharedKernel.Tenancy;

namespace TenantIsolation.Tests.Helpers;

// TST-5 fix: EnsureCreated() was failing because:
//   1. Schema was created in one connection, EnsureCreated ran on another
//   2. StubTenantContext was not injected so HasQueryFilter block was skipped,
//      causing EF to build a model without filters -- table names differed
//
// Fix: create schema + EnsureCreated in one atomic step on a single context
// instance that has a resolved ITenantContext so OnModelCreating runs fully.
public static class DbFactory
{
    private static readonly Guid SchemaTenantId =
        Guid.Parse("00000000-0000-0000-0000-000000000001");

    public static T Create<T>(
        Func<DbContextOptions<T>, T> ctor,
        string connectionString,
        string schemaName)
        where T : DbContext
    {
        var safeSchema = "t" + schemaName.Replace("-", "");

        var connStr = new NpgsqlConnectionStringBuilder(connectionString)
        {
            SearchPath = safeSchema
        }.ConnectionString;

        var opts = new DbContextOptionsBuilder<T>()
            .UseNpgsql(connStr)
            .Options;

        // Build a context with a resolved stub tenant so OnModelCreating
        // runs its full path (HasQueryFilter block included).
        // This ensures EF generates the correct schema/table names.
        using (var setupCtx = BuildWithTenant<T>(connStr, SchemaTenantId))
        {
            // Create schema and tables in one connection on one context.
            setupCtx.Database.ExecuteSqlRaw(
                $"CREATE SCHEMA IF NOT EXISTS \"{safeSchema}\"");
            setupCtx.Database.EnsureCreated();
        }

        // Return a plain context (no tenant) for the test to use.
        // The test controls tenant filtering via repository tenantId parameters.
        return ctor(opts);
    }

    // Constructs a T with (DbContextOptions<T>, ITenantContext?) via reflection.
    // Falls back to single-parameter ctor if the two-parameter one is not found.
    private static T BuildWithTenant<T>(string connectionString, Guid tenantId)
        where T : DbContext
    {
        var opts = new DbContextOptionsBuilder<T>()
            .UseNpgsql(connectionString)
            .Options;

        var stub = new StubTenantContext(tenantId);

        // Find ctor(DbContextOptions<T>, ITenantContext?) or (DbContextOptions<T>, ITenantContext)
        var twoParamCtor = typeof(T).GetConstructors()
            .FirstOrDefault(c =>
            {
                var p = c.GetParameters();
                if (p.Length != 2) return false;
                if (!p[0].ParameterType.IsAssignableFrom(typeof(DbContextOptions<T>)))
                    return false;
                var p1Name = p[1].ParameterType.Name;
                return p1Name.Contains("ITenantContext") || p1Name.Contains("ITenant");
            });

        if (twoParamCtor != null)
        {
            try
            {
                return (T)twoParamCtor.Invoke(new object?[] { opts, stub });
            }
            catch
            {
                // Fall through to single-param ctor
            }
        }

        // Single-param fallback — DbContext without tenant injection
        var oneParamCtor = typeof(T).GetConstructors()
            .FirstOrDefault(c =>
            {
                var p = c.GetParameters();
                return p.Length == 1
                    && p[0].ParameterType.IsAssignableFrom(typeof(DbContextOptions<T>));
            });

        if (oneParamCtor != null)
            return (T)oneParamCtor.Invoke(new object[] { opts });

        throw new InvalidOperationException(
            $"Cannot construct {typeof(T).Name}: no suitable constructor found.");
    }
}

internal sealed class StubTenantContext : ITenantContext
{
    private readonly Guid _tenantId;

    public StubTenantContext(Guid tenantId) => _tenantId = tenantId;

    public Guid   TenantId   => _tenantId;
    public string Slug       => "test-tenant";
    public string Tier       => "standard";
    public bool   IsResolved => true;
}
