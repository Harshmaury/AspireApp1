using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Testcontainers.Kafka;
using Testcontainers.PostgreSql;

namespace Kafka.IntegrationTests.Fixtures;

public sealed class KafkaPostgresFixture : IAsyncLifetime
{
    private readonly KafkaContainer _kafka = new KafkaBuilder()
        .WithImage("confluentinc/cp-kafka:7.6.1")
        .Build();

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("ums_test")
        .WithUsername("ums")
        .WithPassword("ums_pass")
        .Build();

    public string KafkaBootstrap { get; private set; } = default!;
    public string PostgresConnection { get; private set; } = default!;

    public IConfiguration Configuration { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_kafka.StartAsync(), _postgres.StartAsync());
        KafkaBootstrap    = _kafka.GetBootstrapAddress();
        PostgresConnection = _postgres.GetConnectionString();

        Configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:kafka"]    = KafkaBootstrap,
                ["ConnectionStrings:postgres"] = PostgresConnection
            })
            .Build();
    }

    public async Task DisposeAsync()
    {
        await Task.WhenAll(_kafka.DisposeAsync().AsTask(), _postgres.DisposeAsync().AsTask());
    }

    public IConsumer<string, string> CreateConsumer(string groupId) =>
        new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers  = KafkaBootstrap,
            GroupId           = groupId,
            AutoOffsetReset   = AutoOffsetReset.Earliest,
            EnableAutoCommit  = false
        }).Build();

    public async Task<ConsumeResult<string, string>?> ConsumeOneAsync(
        string topic, string groupId, TimeSpan timeout)
    {
        using var consumer = CreateConsumer(groupId);
        consumer.Subscribe(topic);
        var deadline = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < deadline)
        {
            var result = consumer.Consume(TimeSpan.FromMilliseconds(500));
            if (result is not null) return result;
        }
        return null;
    }
}
