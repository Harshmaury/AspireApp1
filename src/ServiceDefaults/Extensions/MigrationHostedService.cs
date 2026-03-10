// ============================================================
// AspireApp1.ServiceDefaults — MigrationHostedService<TDb>
// Runs EF Core database migrations on application startup.
// Retries up to 5 times with 3-second delay (handles cold-start postgres).
//
// Usage in Program.cs:
//   builder.Services.AddHostedService<MigrationHostedService<YourDbContext>>();
// ============================================================
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AspireApp1.ServiceDefaults.Extensions;

/// <summary>
/// Hosted service that runs EF Core migrations at startup with retry logic.
/// Safe for parallel service startup — uses an isolated DI scope per attempt.
/// </summary>
public sealed class MigrationHostedService<TDb>(
    IServiceProvider            services,
    ILogger<MigrationHostedService<TDb>> logger)
    : BackgroundService where TDb : DbContext
{
    private const int MaxRetries   = 5;
    private const int DelaySeconds = 3;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                using var scope = services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<TDb>();
                await db.Database.MigrateAsync(stoppingToken);
                logger.LogInformation(
                    "[Migration] {Db} succeeded on attempt {Attempt}/{Max}",
                    typeof(TDb).Name, attempt, MaxRetries);
                return;
            }
            catch (Exception ex) when (attempt < MaxRetries && !stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(
                    "[Migration] {Db} attempt {Attempt}/{Max} failed: {Msg}. Retrying in {Delay}s...",
                    typeof(TDb).Name, attempt, MaxRetries, ex.Message, DelaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(DelaySeconds), stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "[Migration] {Db} failed after {Max} attempts. Service startup aborted.",
                    typeof(TDb).Name, MaxRetries);
                throw;  // surface to host — crash fast rather than silently run un-migrated
            }
        }
    }
}
