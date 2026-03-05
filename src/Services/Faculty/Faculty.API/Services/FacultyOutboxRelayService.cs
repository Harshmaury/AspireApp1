using Confluent.Kafka;
using Faculty.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using UMS.SharedKernel.Kafka;

namespace Faculty.API.Services;

public sealed class FacultyOutboxRelayService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FacultyOutboxRelayService> _logger;
    private readonly string _bootstrapServers;

    public FacultyOutboxRelayService(
        IServiceScopeFactory scopeFactory,
        ILogger<FacultyOutboxRelayService> logger,
        IConfiguration configuration)
    {
        _scopeFactory     = scopeFactory;
        _logger           = logger;
        _bootstrapServers = configuration.GetConnectionString("kafka") ?? "localhost:9092";
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("Faculty outbox relay started. Bootstrap: {Servers}", _bootstrapServers);
        while (!ct.IsCancellationRequested)
        {
            try { await ProcessPendingMessagesAsync(ct); }
            catch (Exception ex) { _logger.LogWarning(ex, "Faculty outbox relay cycle failed. Retrying in 5s."); }
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        }
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FacultyDbContext>();

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
                    KafkaTopics.FacultyEvents,
                    new Message<string, string> { Key = message.Id.ToString(), Value = message.Payload }, ct);
                message.MarkProcessed();
                _logger.LogInformation("Published faculty outbox message {Id} of type {Type}", message.Id, message.EventType);
            }
            catch (Exception ex)
            {
                message.MarkFailed(ex.Message);
                _logger.LogWarning(ex, "Failed to publish faculty outbox message {Id}", message.Id);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}