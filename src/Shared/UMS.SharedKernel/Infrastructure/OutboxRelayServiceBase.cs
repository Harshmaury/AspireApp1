// UMS — University Management System
// Key: UMS-SHARED-P0-002 (bug fix — TenantId was string.Empty)
// Layer: Shared / Infrastructure
// ─────────────────────────────────────────────────────────────
// BUG FIXED: TenantId was hardcoded to string.Empty in KafkaEventEnvelope.
// Now reads from OutboxMessage.TenantId correctly.
// ─────────────────────────────────────────────────────────────
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using UMS.SharedKernel.Domain;
using UMS.SharedKernel.Kafka;

namespace UMS.SharedKernel.Infrastructure;

/// <summary>
/// Abstract base for all outbox relay background services in UMS.
/// Polls the outbox table, publishes pending messages to Kafka,
/// then marks them as processed — all in a resilient polling loop.
/// <para>
/// Inherit in each service's Infrastructure layer:
/// <code>
///   public sealed class StudentOutboxRelayService
///       : OutboxRelayServiceBase&lt;StudentDbContext&gt;
///   {
///       public StudentOutboxRelayService(...) : base(...) { }
///       protected override string TopicName => KafkaTopics.StudentEvents;
///   }
/// </code>
/// </para>
/// </summary>
public abstract class OutboxRelayServiceBase<TDbContext> : BackgroundService
    where TDbContext : DbContext
{
    private readonly IServiceScopeFactory    _scopeFactory;
    private readonly IProducer<Null, string> _producer;
    private readonly ILogger                 _logger;
    private readonly TimeSpan                _pollingInterval;
    private const    int                     BatchSize = 50;

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

    /// <summary>
    /// Kafka topic to publish to — use <see cref="KafkaTopics"/> constants.
    /// </summary>
    protected abstract string TopicName { get; }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[OutboxRelay:{Topic}] Started. Polling every {Interval}s",
            TopicName, _pollingInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "[OutboxRelay:{Topic}] Relay cycle error", TopicName);
            }

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
            .Take(BatchSize)
            .ToListAsync(ct);

        if (pending.Count == 0) return;

        _logger.LogDebug("[OutboxRelay:{Topic}] Relaying {Count} messages",
            TopicName, pending.Count);

        foreach (var msg in pending)
        {
            // ── FIX: read TenantId from the outbox message, not string.Empty ──
            var tenantId = msg.TenantId.HasValue
                ? msg.TenantId.Value
                : Guid.Empty;

            var envelope = KafkaEventEnvelope.Create(
                eventType:    msg.EventType,
                tenantId:     tenantId,
                regionOrigin: msg.RegionOrigin ?? string.Empty,
                payload:      msg.Payload);

            var json = JsonSerializer.Serialize(envelope);

            await _producer.ProduceAsync(
                TopicName,
                new Message<Null, string> { Value = json },
                ct);

            msg.ProcessedAt = DateTimeOffset.UtcNow;

            _logger.LogDebug(
                "[OutboxRelay:{Topic}] Published {EventType} | MsgId={MsgId} TenantId={TenantId}",
                TopicName, msg.EventType, msg.Id, tenantId);
        }

        await db.SaveChangesAsync(ct);
    }
}
