using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql;
using UMS.SharedKernel.Tenancy;

namespace TenantIsolation.Tests.Helpers;

// TST-6 fix: EnsureCreated() fails silently when an entity configuration
// declares a PostgreSQL system column (xmin / xid) as a concurrency token.
// Postgres rejects any DDL that tries to ADD or reference xmin as a regular
// column, so EnsureCreated() leaves the schema empty — producing the
// "relation does not exist" error seen in Student, Fee, Examination, etc.
//
// Root cause chain:
//   StudentConfiguration declares:
//     builder.Property<uint>("xmin").HasColumnName("xmin").HasColumnType("xid")
//   EnsureCreated() emits:
//     CREATE TABLE "Students" (..., xmin xid, ...)
//   Postgres rejects because xmin is a built-in system column.
//   EnsureCreated() swallows the error — schema is created but table is not.
//   SaveChanges() then throws: 42P01 relation "Students" does not exist.
//
// Fix strategy:
//   1. Try EnsureCreated() normally.
//   2. After the call, verify that the expected tables actually exist.
//   3. For any table that is missing, create it with raw SQL that omits
//      system columns (xmin, ctid, etc.) — Postgres provides these automatically.
//
// This approach requires no changes to production entity configurations.
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

        // Build setup context with resolved tenant so OnModelCreating
        // runs the HasQueryFilter branch — needed for correct model shape.
        using var setupCtx = BuildWithTenant<T>(connStr, SchemaTenantId);

        // Create schema first.
        setupCtx.Database.ExecuteSqlRaw(
            $"CREATE SCHEMA IF NOT EXISTS \"{safeSchema}\"");

        // Attempt EnsureCreated — may fail silently for xid columns.
        try { setupCtx.Database.EnsureCreated(); } catch { /* see step below */ }

        // Verify every entity table exists; create missing ones via raw DDL
        // that omits PostgreSQL system columns (xmin, ctid, oid, etc.).
        EnsureTablesExist(setupCtx, safeSchema);

        return ctor(opts);
    }

    // For each entity type mapped to a table, checks whether that table exists
    // in the given schema. If not, emits a minimal CREATE TABLE statement that
    // covers all non-system columns declared in the EF model.
    private static void EnsureTablesExist(DbContext ctx, string schema)
    {
        // System column names Postgres owns — never emit these in DDL.
        var systemColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "xmin", "xmax", "cmin", "cmax", "ctid", "oid", "tableoid"
        };

        var model = ctx.Model;

        foreach (var entityType in model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            if (tableName == null) continue;

            // Check if table already exists in this schema.
            var exists = ctx.Database.ExecuteSqlRaw(
                $"SELECT 1 FROM information_schema.tables " +
                $"WHERE table_schema = '{schema}' AND table_name = '{tableName}'") >= 0;

            // ExecuteSqlRaw returns rows affected — use a scalar check instead.
            var tableExists = CheckTableExists(ctx, schema, tableName);
            if (tableExists) continue;

            // Build column list from EF model, skipping system columns.
            var columns = new List<string>();

            foreach (var prop in entityType.GetProperties())
            {
                var colName = prop.GetColumnName();
                if (systemColumns.Contains(colName)) continue;

                var colType = prop.GetColumnType() ?? MapClrType(prop.ClrType);
                var nullable = prop.IsNullable ? "" : " NOT NULL";
                var defaultSql = prop.GetDefaultValueSql();
                var defaultClause = defaultSql != null ? $" DEFAULT {defaultSql}" : "";

                // Primary key identity
                if (prop.ValueGenerated == Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd
                    && prop.ClrType == typeof(Guid))
                    defaultClause = " DEFAULT gen_random_uuid()";

                columns.Add($"\"{colName}\" {colType}{nullable}{defaultClause}");
            }

            // Primary key constraint
            var pkProps = entityType.FindPrimaryKey()?.Properties
                .Select(p => $"\"{p.GetColumnName()}\"")
                .ToList();
            if (pkProps?.Count > 0)
                columns.Add($"PRIMARY KEY ({string.Join(", ", pkProps)})");

            if (columns.Count == 0) continue;

            var ddl = $"CREATE TABLE IF NOT EXISTS \"{schema}\".\"{tableName}\" " +
                      $"({string.Join(", ", columns)})";

            try
            {
                ctx.Database.ExecuteSqlRaw(ddl);
            }
            catch (Exception ex)
            {
                // Log and continue — a partial schema is better than a crash.
                Console.WriteLine(
                    $"[DbFactory] Warning: could not create table '{tableName}': {ex.Message}");
            }
        }

        // Indexes — best effort
        foreach (var entityType in model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            if (tableName == null) continue;

            foreach (var index in entityType.GetIndexes())
            {
                var indexCols = index.Properties
                    .Select(p => p.GetColumnName())
                    .Where(c => !systemColumns.Contains(c))
                    .Select(c => $"\"{c}\"")
                    .ToList();

                if (indexCols.Count == 0) continue;

                var unique = index.IsUnique ? "UNIQUE " : "";
                var indexName = index.GetDatabaseName()
                    ?? $"ix_{tableName}_{string.Join("_", indexCols)}";
                var ddl = $"CREATE {unique}INDEX IF NOT EXISTS \"{indexName}\" " +
                          $"ON \"{schema}\".\"{tableName}\" ({string.Join(", ", indexCols)})";
                try { ctx.Database.ExecuteSqlRaw(ddl); } catch { /* best effort */ }
            }
        }
    }

    private static bool CheckTableExists(DbContext ctx, string schema, string tableName)
    {
        using var conn = ctx.Database.GetDbConnection();
        var wasOpen = conn.State == System.Data.ConnectionState.Open;
        if (!wasOpen) conn.Open();
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                $"SELECT COUNT(1) FROM information_schema.tables " +
                $"WHERE table_schema = '{schema}' AND table_name = '{tableName}'";
            var result = cmd.ExecuteScalar();
            return Convert.ToInt64(result) > 0;
        }
        finally
        {
            if (!wasOpen) conn.Close();
        }
    }

    private static string MapClrType(Type t)
    {
        if (t == typeof(Guid) || t == typeof(Guid?))        return "uuid";
        if (t == typeof(string))                             return "text";
        if (t == typeof(int) || t == typeof(int?))          return "integer";
        if (t == typeof(long) || t == typeof(long?))        return "bigint";
        if (t == typeof(bool) || t == typeof(bool?))        return "boolean";
        if (t == typeof(DateTime) || t == typeof(DateTime?))return "timestamp with time zone";
        if (t == typeof(DateOnly) || t == typeof(DateOnly?)) return "date";
        if (t == typeof(decimal) || t == typeof(decimal?))  return "numeric";
        if (t == typeof(double) || t == typeof(double?))    return "double precision";
        if (t == typeof(float) || t == typeof(float?))      return "real";
        if (t == typeof(uint) || t == typeof(uint?))        return "xid";
        return "text";
    }

    private static T BuildWithTenant<T>(string connectionString, Guid tenantId)
        where T : DbContext
    {
        var opts = new DbContextOptionsBuilder<T>()
            .UseNpgsql(connectionString)
            .Options;

        var stub = new StubTenantContext(tenantId);

        var twoParam = typeof(T).GetConstructors()
            .FirstOrDefault(c =>
            {
                var p = c.GetParameters();
                if (p.Length != 2) return false;
                if (!p[0].ParameterType.IsAssignableFrom(typeof(DbContextOptions<T>))) return false;
                var p1 = p[1].ParameterType.Name;
                return p1.Contains("ITenantContext") || p1.Contains("ITenant");
            });

        if (twoParam != null)
        {
            try { return (T)twoParam.Invoke(new object?[] { opts, stub }); }
            catch { /* fall through */ }
        }

        var oneParam = typeof(T).GetConstructors()
            .FirstOrDefault(c =>
            {
                var p = c.GetParameters();
                return p.Length == 1
                    && p[0].ParameterType.IsAssignableFrom(typeof(DbContextOptions<T>));
            });

        if (oneParam != null) return (T)oneParam.Invoke(new object[] { opts });

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
