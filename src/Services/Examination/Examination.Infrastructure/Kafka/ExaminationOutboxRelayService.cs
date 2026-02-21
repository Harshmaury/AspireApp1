using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Examination.Infrastructure.Kafka;

public sealed class ExaminationOutboxRelayService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExaminationOutboxRelayService> _logger;
    private readonly string _bootstrapServers;

    public ExaminationOutboxRelayService(
        IServiceScopeFactory scopeFactory,
        ILogger<ExaminationOutboxRelayService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _bootstrapServers = configuration.GetConnectionString("kafka") ?? "localhost:9092";
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<Examination.Infrastructure.Persistence.ExaminationDbContext>();
                var messages = await db.OutboxMessages
                    .Where(m => m.ProcessedAt == null && m.RetryCount < 5)
                    .OrderBy(m => m.CreatedAt)
                    .Take(20)
                    .ToListAsync(ct);

                if (messages.Any())
                {
                    var config = new ProducerConfig { BootstrapServers = _bootstrapServers, SecurityProtocol = SecurityProtocol.Plaintext };
                    using var producer = new ProducerBuilder<string, string>(config).Build();
                    foreach (var msg in messages)
                    {
                        try
                        {
                            await producer.ProduceAsync("examination-events", new Message<string, string> { Key = msg.Id.ToString(), Value = msg.Payload }, ct);
                            msg.ProcessedAt = DateTime.UtcNow;
                        }
                        catch { msg.RetryCount++; }
                    }
                    await db.SaveChangesAsync(ct);
                }
            }
            catch (Exception ex) { _logger.LogError(ex, "Examination outbox relay error"); }
            await Task.Delay(5000, ct);
        }
    }
}
