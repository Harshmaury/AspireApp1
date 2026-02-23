using Testcontainers.PostgreSql;
using Xunit;
using Microsoft.EntityFrameworkCore;

namespace AppHost.IntegrationTests.Helpers;

public class ServiceFixture<TProgram, TDbContext> : IAsyncLifetime
    where TProgram : class
    where TDbContext : DbContext
{
    private readonly string _dbKey = typeof(TDbContext).Name.Replace("DbContext", "Db");

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("ums_test")
        .WithUsername("ums")
        .WithPassword("ums_pass")
        .Build();

    public HttpClient Client { get; private set; } = null!;
    private ServiceWebFactory<TProgram, TDbContext> _factory = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        var connStr = _container.GetConnectionString();

        // Must set BEFORE factory builds — AddNpgSqlHealthCheck reads from IConfiguration
        Environment.SetEnvironmentVariable($"ConnectionStrings__{_dbKey}", connStr);

        _factory = new ServiceWebFactory<TProgram, TDbContext>(connStr);
        Client = _factory.CreateClient();

        // Simulate gateway forwarded headers — services read these instead of JWT
        Client.DefaultRequestHeaders.Add("X-Tenant-Id", TestTenant.Id.ToString());
        Client.DefaultRequestHeaders.Add("X-User-Id", Guid.NewGuid().ToString());
    }

    public async Task DisposeAsync()
    {
        Environment.SetEnvironmentVariable($"ConnectionStrings__{_dbKey}", null);
        _factory?.Dispose();
        await _container.DisposeAsync();
    }
}