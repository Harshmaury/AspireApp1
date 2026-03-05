using Confluent.Kafka;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using UMS.SharedKernel.Kafka;

namespace Identity.API.Services;

public sealed class OutboxRelayService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxRelayService> _logger;
    private readonly string _bootstrapServers;
    private IProducer<string, string>? _producer;

    public OutboxRelayService(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxRelayService> logger,
        IConfiguration configuration)
    {
        _scopeFactory     = scopeFactory;
        _logger           = logger;
        _bootstrapServers = configuration.GetConnectionString("kafka") ?? "localhost:9092";
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = _bootstrapServers,
            SecurityProtocol = SecurityProtocol.Plaintext
        };
        _producer = new ProducerBuilder<string, string>(config).Build();

        _logger.LogInformation("Outbox relay started. Bootstrap: {Servers}", _bootstrapServers);

        while (!ct.IsCancellationRequested)
        {
            try { await ProcessPendingMessagesAsync(ct); }
            catch (Exception ex) { _logger.LogWarning(ex, "Outbox relay cycle failed. Retrying in 5s."); }
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        }
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var messages = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < 5)
            .OrderBy(m => m.CreatedAt)
            .Take(20)
            .ToListAsync(ct);

        if (!messages.Any())
        {
            var dead = await db.OutboxMessages
                .CountAsync(m => m.ProcessedAt == null && m.RetryCount >= 5, ct);
            if (dead > 0)
                _logger.LogError("OUTBOX DEAD LETTERS: {Count} messages permanently failed", dead);
            return;
        }

        foreach (var message in messages)
        {
            try
            {
                await _producer!.ProduceAsync(
                    KafkaTopics.IdentityEvents,
                    new Message<string, string> { Key = message.Id.ToString(), Value = System.Text.Json.JsonSerializer.Serialize(UMS.SharedKernel.Kafka.KafkaEventEnvelope.Create(message.EventType, Guid.Empty, "default", message.Payload)) }, ct);
                _logger.LogInformation("Published outbox message {Id} of type {Type}", message.Id, message.EventType);
            }
            catch (Exception ex)
            {
                message.MarkFailed(ex.Message);
                _logger.LogWarning(ex, "Failed to publish outbox message {Id}", message.Id);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    public override void Dispose()
    {
        _producer?.Flush(TimeSpan.FromSeconds(5));
        _producer?.Dispose();
        base.Dispose();
    }
}
