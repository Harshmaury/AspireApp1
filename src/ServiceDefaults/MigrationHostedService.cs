// AspireApp1.ServiceDefaults/MigrationHostedService.cs
// Generic EF Core migration runner.
// Register in each service Program.cs:
//   builder.Services.AddHostedService<MigrationHostedService<MyDbContext>>();

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AspireApp1.ServiceDefaults;

public sealed class MigrationHostedService<TContext> : IHostedService
    where TContext : DbContext
{
    private readonly IServiceScopeFactory                    _scopeFactory;
    private readonly ILogger<MigrationHostedService<TContext>> _logger;

    public MigrationHostedService(
        IServiceScopeFactory                      scopeFactory,
        ILogger<MigrationHostedService<TContext>> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[Migration:{Context}] Applying EF Core migrations...", typeof(TContext).Name);
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TContext>();
            await db.Database.MigrateAsync(cancellationToken);
            _logger.LogInformation("[Migration:{Context}] Migrations applied successfully.", typeof(TContext).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Migration:{Context}] Migration failed.", typeof(TContext).Name);
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
