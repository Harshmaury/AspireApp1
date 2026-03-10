// UMS.SharedKernel/Infrastructure/OutboxRelayServiceBase.cs
// Property names matched from actual OutboxMessage in this solution.
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UMS.SharedKernel.Domain;
using UMS.SharedKernel.Kafka;

namespace UMS.SharedKernel.Infrastructure;

public abstract class OutboxRelayServiceBase<TDbContext> : BackgroundService
    where TDbContext : DbContext
{
    private readonly IServiceScopeFactory    _scopeFactory;
    private readonly IProducer<Null, string> _producer;
    private readonly ILogger                 _logger;
    private readonly TimeSpan                _pollingInterval;

    protected OutboxRelayServiceBase(
        IServiceScopeFactory    scopeFactory,
        IProducer<Null, string> producer,
        ILogger                 logger,
        TimeSpan?               pollingInterval = null)
    {
        _scopeFactory    = scopeFactory;
        _producer        = producer;
        _logger          = logger;
        _pollingInterval = pollingInterval ?? TimeSpan.FromSeconds(5);
    }

    protected abstract string TopicName { get; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[OutboxRelay:{Topic}] Started", TopicName);
        while (!stoppingToken.IsCancellationRequested)
        {
            try   { await ProcessBatchAsync(stoppingToken); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            { _logger.LogError(ex, "[OutboxRelay:{Topic}] Relay cycle error", TopicName); }
            await Task.Delay(_pollingInterval, stoppingToken);
        }
        _logger.LogInformation("[OutboxRelay:{Topic}] Stopped", TopicName);
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TDbContext>();

        var pending = await db.Set<OutboxMessage>()
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.OccurredAt)
            .Take(50)
            .ToListAsync(ct);

        if (pending.Count == 0) return;

        _logger.LogDebug("[OutboxRelay:{Topic}] Relaying {Count} messages", TopicName, pending.Count);

        foreach (var msg in pending)
        {
            var envelope = new KafkaEventEnvelope
            {
                EventId    = msg.Id,
                EventType  = msg.EventType,
                OccurredAt = msg.OccurredAt.UtcDateTime,
                TenantId   = string.Empty,
                Payload    = msg.Payload
            };

            var json = JsonSerializer.Serialize(envelope);
            await _producer.ProduceAsync(TopicName, new Message<Null, string> { Value = json }, ct);
            msg.ProcessedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct);
    }
}



