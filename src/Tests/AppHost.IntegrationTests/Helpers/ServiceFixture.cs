using Testcontainers.PostgreSql;
using Xunit;
using Microsoft.EntityFrameworkCore;

namespace AppHost.IntegrationTests.Helpers;

public class ServiceFixture<TProgram, TDbContext> : IAsyncLifetime
    where TProgram : class
    where TDbContext : DbContext
{
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
        _factory = new ServiceWebFactory<TProgram, TDbContext>(_container.GetConnectionString());
        // Do NOT call EnsureCreatedAsync — Program.cs already calls db.Database.Migrate() at startup
        Client = _factory.CreateClient(); // this triggers Program.cs startup → Migrate() runs
    }

    public async Task DisposeAsync()
    {
        _factory?.Dispose();
        await _container.DisposeAsync();
    }
}
