using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Notification.Infrastructure.Kafka;

public abstract class KafkaConsumerBase<TEvent> : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _logger;
    private readonly string _topic;
    private readonly string _groupId;
    private readonly string _bootstrapServers;

    protected KafkaConsumerBase(
        IServiceScopeFactory scopeFactory,
        ILogger logger,
        string topic,
        string groupId,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _topic = topic;
        _groupId = groupId;
        _bootstrapServers = configuration.GetConnectionString("kafka") ?? "localhost:9092";
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = _groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(_topic);
        _logger.LogInformation("Kafka consumer started for topic {Topic}", _topic);

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
                    consumer.Commit(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process message from topic {Topic} offset {Offset}", _topic, result.Offset);
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kafka consumer error on topic {Topic}", _topic);
                await Task.Delay(3000, ct);
            }
        }

        consumer.Close();
        _logger.LogInformation("Kafka consumer stopped for topic {Topic}", _topic);
    }

    protected abstract Task ProcessAsync(TEvent eventData, IServiceProvider services, CancellationToken ct);
}
