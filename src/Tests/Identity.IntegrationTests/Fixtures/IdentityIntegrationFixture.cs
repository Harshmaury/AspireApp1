using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;
using Identity.Infrastructure.Persistence;

namespace Identity.IntegrationTests.Fixtures;

public sealed class IdentityIntegrationFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("identity_test")
        .WithUsername("test_user")
        .WithPassword("test_pass")
        .Build();

    public HttpClient Client { get; private set; } = default!;
    private WebApplicationFactory<Program> _factory = default!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var connStr = _postgres.GetConnectionString();

        // Set BEFORE factory builds — read by AddNpgsqlHealthCheck during service registration
        Environment.SetEnvironmentVariable("ConnectionStrings__IdentityDb", connStr);

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(host =>
            {
                host.UseEnvironment("Testing");
                host.ConfigureServices(services =>
                {
                    // Remove all hosted services (Kafka relay, etc.)
                    var hostedDescriptors = services
                        .Where(d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService))
                        .ToList();
                    foreach (var d in hostedDescriptors) services.Remove(d);

                    // Remove existing DbContext registration
                    var dbDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (dbDescriptor is not null) services.Remove(dbDescriptor);

                    // Re-register WITH UseOpenIddict() — required for OpenIddict stores
                    services.AddDbContext<ApplicationDbContext>((sp, options) =>
                    {
                        options.UseNpgsql(connStr,
                            npgsql => npgsql.MigrationsAssembly(
                                typeof(ApplicationDbContext).Assembly.FullName));
                        options.UseOpenIddict();
                    });
                });
            });

        // Apply EF migrations so all tables including OpenIddict exist
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        Client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__IdentityDb", null);
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
