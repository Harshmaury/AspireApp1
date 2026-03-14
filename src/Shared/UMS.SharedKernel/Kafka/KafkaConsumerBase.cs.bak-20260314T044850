// ============================================================
// UMS.SharedKernel — KafkaConsumerBase<TEvent>
// Generic base for all Kafka consumer BackgroundServices in UMS.
//
// Consumer group ID format enforced by KAFKA-001 rule:
//   {serviceName}.{REGION_ID}.{purpose}
// ============================================================
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace UMS.SharedKernel.Kafka;

/// <summary>
/// Abstract base for all Kafka consumer background services in UMS.
/// Handles connection lifecycle, offset commit, error back-off,
/// and KAFKA-001 consumer group scoping rule enforcement.
/// </summary>
/// <typeparam name="TEvent">The deserialized event type consumed from Kafka.</typeparam>
public abstract class KafkaConsumerBase<TEvent> : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger              _logger;
    private readonly string               _topic;
    private readonly string               _groupId;
    private readonly string               _bootstrapServers;

    /// <summary>
    /// Initializes the consumer with full KAFKA-001-compliant group ID.
    /// </summary>
    /// <param name="scopeFactory">DI scope factory for per-message scopes.</param>
    /// <param name="logger">Logger (use the concrete type's logger).</param>
    /// <param name="topic">Kafka topic to subscribe to.</param>
    /// <param name="serviceName">Short service name, e.g. "notification".</param>
    /// <param name="purpose">Consumer purpose, e.g. "email-dispatch".</param>
    /// <param name="configuration">App configuration (reads kafka + REGION_ID).</param>
    protected KafkaConsumerBase(
        IServiceScopeFactory scopeFactory,
        ILogger              logger,
        string               topic,
        string               serviceName,
        string               purpose,
        IConfiguration       configuration)
    {
        _scopeFactory     = scopeFactory;
        _logger           = logger;
        _topic            = topic;
        _bootstrapServers = configuration.GetConnectionString("kafka") ?? "localhost:9092";

        var regionId = configuration["REGION_ID"]
            ?? throw new InvalidOperationException(
                "REGION_ID is not configured. " +
                "Ensure k8s overlay configmap-patch.yaml injects REGION_ID into the pod.");

        _groupId = BuildGroupId(serviceName, regionId, purpose);

        _logger.LogInformation(
            "Kafka consumer configured: topic={Topic} group={GroupId}", _topic, _groupId);
    }

    /// <summary>
    /// Constructs a KAFKA-001-compliant consumer group ID.
    /// Format: {serviceName}.{regionId}.{purpose}
    /// </summary>
    public static string BuildGroupId(string serviceName, string regionId, string purpose)
        => $"{serviceName}.{regionId}.{purpose}";

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId          = _groupId,
            AutoOffsetReset  = AutoOffsetReset.Earliest,
            EnableAutoCommit = false   // we commit only on successful processing
        }).Build();

        consumer.Subscribe(_topic);

        _logger.LogInformation(
            "Kafka consumer started. Topic={Topic} GroupId={GroupId}", _topic, _groupId);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(TimeSpan.FromMilliseconds(500));
                if (result is null) continue;

                try
                {
                    var eventData = JsonSerializer.Deserialize<TEvent>(result.Message.Value);
                    if (eventData is not null)
                    {
                        using var scope = _scopeFactory.CreateScope();
                        await ProcessAsync(eventData, scope.ServiceProvider, ct);
                    }
                    consumer.Commit(result);   // manual commit after successful processing
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to process Kafka message. " +
                        "Topic={Topic} Partition={Partition} Offset={Offset}",
                        _topic, result.Partition, result.Offset);
                    // Offset is NOT committed — message will be re-consumed after restart
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Kafka consumer fatal error on topic {Topic}. Retrying in 3s.", _topic);
                await Task.Delay(3_000, ct);
            }
        }

        consumer.Close();
        _logger.LogInformation(
            "Kafka consumer stopped. Topic={Topic} GroupId={GroupId}", _topic, _groupId);
    }

    /// <summary>
    /// Process a single deserialized event.
    /// A new DI scope is provided per message — resolve scoped services freely.
    /// </summary>
    protected abstract Task ProcessAsync(
        TEvent            eventData,
        IServiceProvider  services,
        CancellationToken ct);
}
