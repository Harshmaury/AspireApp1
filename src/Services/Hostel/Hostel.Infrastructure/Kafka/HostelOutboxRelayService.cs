using Confluent.Kafka;
using Hostel.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
namespace Hostel.Infrastructure.Kafka;
public sealed class HostelOutboxRelayService(IServiceScopeFactory scopeFactory,
    IProducer<string, string> producer, ILogger<HostelOutboxRelayService> logger)
    : BackgroundService
{
    private const string Topic = "hostel-events";
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try { await ProcessBatchAsync(ct); }
            catch (Exception ex) { logger.LogError(ex, "Hostel outbox relay error"); }
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        }
    }
    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Hostel.Infrastructure.Persistence.HostelDbContext>();
        var msgs = await db.OutboxMessages
            .Where(x => x.ProcessedAt == null && x.RetryCount < OutboxMessage.MaxRetries)
            .OrderBy(x => x.CreatedAt).Take(20).ToListAsync(ct);
        foreach (var msg in msgs)
        {
            try
            {
                await producer.ProduceAsync(Topic, new Message<string, string>
                    { Key = msg.Id.ToString(), Value = msg.Payload }, ct);
                msg.ProcessedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                msg.RetryCount++;
                logger.LogWarning(ex, "Failed to publish outbox message {Id}, retry {Retry}", msg.Id, msg.RetryCount);
            }
        }
        await db.SaveChangesAsync(ct);
    }
}
