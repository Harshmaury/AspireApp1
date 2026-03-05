using Microsoft.EntityFrameworkCore;

public class MigrationHostedService<TDb>(IServiceProvider services, ILogger<MigrationHostedService<TDb>> logger)
    : BackgroundService where TDb : DbContext
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        const int maxRetries = 5;
        const int delaySeconds = 3;

        for (int i = 1; i <= maxRetries; i++)
        {
            try
            {
                using var scope = services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<TDb>();
                await db.Database.MigrateAsync(stoppingToken);
                logger.LogInformation("[Migration] {Db} succeeded on attempt {Attempt}", typeof(TDb).Name, i);
                return;
            }
            catch (Exception ex) when (i < maxRetries && !stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning("[Migration] {Db} attempt {Attempt}/{Max} failed: {Msg}. Retrying in {Delay}s...",
                    typeof(TDb).Name, i, maxRetries, ex.Message, delaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
            }
        }
    }
}
