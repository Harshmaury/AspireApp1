using Confluent.Kafka;
using Examination.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UMS.SharedKernel.Kafka;

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
        _scopeFactory     = scopeFactory;
        _logger           = logger;
        _bootstrapServers = configuration.GetConnectionString("kafka") ?? "localhost:9092";
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("Examination outbox relay started. Bootstrap: {Servers}", _bootstrapServers);
        while (!ct.IsCancellationRequested)
        {
            try { await ProcessPendingMessagesAsync(ct); }
            catch (Exception ex) { _logger.LogWarning(ex, "Examination outbox relay cycle failed. Retrying in 5s."); }
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        }
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExaminationDbContext>();

        var messages = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < 5)
            .OrderBy(m => m.CreatedAt)
            .Take(20)
            .ToListAsync(ct);

        if (!messages.Any()) return;

        var config = new ProducerConfig { BootstrapServers = _bootstrapServers, SecurityProtocol = SecurityProtocol.Plaintext };
        using var producer = new ProducerBuilder<string, string>(config).Build();

        foreach (var message in messages)
        {
            try
            {
                await producer.ProduceAsync(
                    KafkaTopics.ExaminationEvents,
                    new Message<string, string> { Key = message.Id.ToString(), Value = message.Payload }, ct);
                message.MarkProcessed();
                _logger.LogInformation("Published examination outbox message {Id} of type {Type}", message.Id, message.EventType);
            }
            catch (Exception ex)
            {
                message.MarkFailed(ex.Message);
                _logger.LogWarning(ex, "Failed to publish examination outbox message {Id}", message.Id);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}