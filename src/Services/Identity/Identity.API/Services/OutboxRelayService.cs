// src/Services/Identity/Identity.API/Services/OutboxRelayService.cs
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
            SecurityProtocol = SecurityProtocol.Plaintext,
            // Guarantee ordering within a partition (by tenantId key)
            EnableIdempotence = true,
            Acks = Acks.All,
            MessageSendMaxRetries = 3
        };
        _producer = new ProducerBuilder<string, string>(config).Build();

        _logger.LogInformation(
            "Outbox relay started. Bootstrap: {Servers}", _bootstrapServers);

        while (!ct.IsCancellationRequested)
        {
            try { await ProcessPendingMessagesAsync(ct); }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Outbox relay cycle failed. Retrying in 5s.");
            }
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
                _logger.LogError(
                    "OUTBOX DEAD LETTERS: {Count} messages permanently failed. " +
                    "Manual intervention required.", dead);
            return;
        }

        var publishedCount = 0;
        foreach (var message in messages)
        {
            try
            {
                // BUG-002 FIX: Use message.TenantId from outbox row (was always Guid.Empty)
                var envelope = KafkaEventEnvelope.Create(
                    message.EventType,
                    message.TenantId,   // ← fixed
                    "default",
                    message.Payload);

                await _producer!.ProduceAsync(
                    KafkaTopics.IdentityEvents,
                    new Message<string, string>
                    {
                        // Key by TenantId for partition ordering
                        Key   = message.TenantId.ToString(),
                        Value = System.Text.Json.JsonSerializer.Serialize(envelope)
                    }, ct);

                message.MarkProcessed();
                publishedCount++;

                _logger.LogInformation(
                    "Published outbox {Id} type={Type} tenant={TenantId}",
                    message.Id, message.EventType, message.TenantId);
            }
            catch (ProduceException<string, string> ex)
            {
                message.MarkFailed(ex.Error.Reason);
                _logger.LogWarning(ex,
                    "Failed to publish outbox {Id} (retry {Retry})",
                    message.Id, message.RetryCount);
            }
        }

        if (publishedCount > 0)
            await db.SaveChangesAsync(ct);
    }

    public override void Dispose()
    {
        _producer?.Flush(TimeSpan.FromSeconds(5));
        _producer?.Dispose();
        base.Dispose();
    }
}