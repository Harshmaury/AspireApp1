using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

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

        // Run migrations so test schema matches production exactly (fixes TST-2)
        await _factory.EnsureCreatedAsync();

        // Simulate gateway forwarded headers — services read these instead of JWT
        Client.DefaultRequestHeaders.Add("X-Tenant-Id", TestTenant.Id.ToString());
        Client.DefaultRequestHeaders.Add("X-User-Id", Guid.NewGuid().ToString());
    }

    // Allows tests to open a DbContext scope and verify DB state directly
    public IServiceScope CreateScope() => _factory.Services.CreateScope();

    public async Task DisposeAsync()
    {
        Environment.SetEnvironmentVariable($"ConnectionStrings__{_dbKey}", null);
        _factory?.Dispose();
        await _container.DisposeAsync();
    }
}
