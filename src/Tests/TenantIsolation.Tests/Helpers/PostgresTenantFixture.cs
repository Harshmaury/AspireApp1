using Testcontainers.PostgreSql;
using Xunit;

namespace TenantIsolation.Tests.Helpers;

// TST-4 fix: one real Postgres container shared across all tenant isolation tests.
// Each test gets its own schema (named by Guid) so data never bleeds between tests.
[CollectionDefinition("TenantIsolation")]
public sealed class TenantIsolationCollection : ICollectionFixture<PostgresTenantFixture> { }

public sealed class PostgresTenantFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("tenant_isolation_test")
        .WithUsername("ums")
        .WithPassword("ums_pass")
        .Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();
}
