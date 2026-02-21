using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
namespace Fee.Infrastructure.Kafka;
public sealed class FeeOutboxRelayService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FeeOutboxRelayService> _logger;
    public FeeOutboxRelayService(IServiceScopeFactory scopeFactory, ILogger<FeeOutboxRelayService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<Fee.Infrastructure.Persistence.FeeDbContext>();
                var messages = await db.OutboxMessages
                    .Where(m => m.ProcessedAt == null && m.RetryCount < 5)
                    .OrderBy(m => m.CreatedAt)
                    .Take(20)
                    .ToListAsync(ct);
                if (messages.Any())
                {
                    var config = new ProducerConfig { BootstrapServers = "localhost:9092" };
                    using var producer = new ProducerBuilder<string, string>(config).Build();
                    foreach (var msg in messages)
                    {
                        try
                        {
                            await producer.ProduceAsync("fee-events", new Message<string, string> { Key = msg.Id.ToString(), Value = msg.Payload }, ct);
                            msg.ProcessedAt = DateTime.UtcNow;
                        }
                        catch { msg.RetryCount++; }
                    }
                    await db.SaveChangesAsync(ct);
                }
            }
            catch (Exception ex) { _logger.LogError(ex, "Fee outbox relay error"); }
            await Task.Delay(5000, ct);
        }
    }
}
