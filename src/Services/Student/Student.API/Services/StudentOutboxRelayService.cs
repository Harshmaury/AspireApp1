// UMS - University Management System
// Key:     UMS-SHARED-P0-003-RESIDUAL
// Layer:   API (pending move to Infrastructure per UMS-SVC-P0-001-STU)
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Student.Infrastructure.Persistence;
using UMS.SharedKernel.Kafka;

namespace Student.API.Services;

public sealed class StudentOutboxRelayService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StudentOutboxRelayService> _logger;
    private readonly IProducer<string, string> _producer;

    public StudentOutboxRelayService(
        IServiceScopeFactory scopeFactory,
        ILogger<StudentOutboxRelayService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;

        var bootstrapServers = configuration.GetConnectionString("kafka") ?? "localhost:9092";
        _producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            SecurityProtocol = SecurityProtocol.Plaintext
        }).Build();

        _logger.LogInformation("Student outbox relay started. Bootstrap: {Servers}", bootstrapServers);
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try { await ProcessPendingMessagesAsync(ct); }
            catch (Exception ex) { _logger.LogWarning(ex, "Student outbox relay cycle failed. Retrying in 5s."); }
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        }
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StudentDbContext>();

        var messages = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < 5)
            .OrderBy(m => m.CreatedAt)
            .Take(20)
            .ToListAsync(ct);

        if (!messages.Any()) return;

        foreach (var message in messages)
        {
            try
            {
                await _producer.ProduceAsync(
                    KafkaTopics.StudentEvents,
                    new Message<string, string>
                    {
                        Key   = message.Id.ToString(),
                        Value = System.Text.Json.JsonSerializer.Serialize(
                            new KafkaEventEnvelope { EventType = message.EventType, Payload = message.Payload })
                    }, ct);

                message.ProcessedAt = DateTimeOffset.UtcNow;
                _logger.LogInformation("Published student outbox message {Id} of type {Type}",
                    message.Id, message.EventType);
            }
            catch (Exception ex)
            {
                message.RetryCount++;
                message.Error = ex.Message;
                _logger.LogWarning(ex, "Failed to publish student outbox message {Id}", message.Id);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    public override void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
        base.Dispose();
    }
}
