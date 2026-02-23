using Confluent.Kafka;
using FluentAssertions;
using Hostel.Domain.Common;
using Hostel.Infrastructure.Kafka;
using Hostel.Infrastructure.Persistence;
using Kafka.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kafka.IntegrationTests.Tests;

[Collection("KafkaPostgres")]
public sealed class HostelOutboxRelayTests(KafkaPostgresFixture fx)
{
    private HostelDbContext BuildDb()
    {
        var opts = new DbContextOptionsBuilder<HostelDbContext>()
            .UseNpgsql(fx.PostgresConnection).Options;
        var db = new HostelDbContext(opts);
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public async Task Relay_publishes_outbox_message_to_hostel_events_topic()
    {
        await using var db = BuildDb();
        var msg = new OutboxMessage
        {
            EventType = "Hostel.RoomAllocated",
            Payload   = """{"roomId":"r-1"}"""
        };
        db.OutboxMessages.Add(msg);
        await db.SaveChangesAsync();

        var svc = new ServiceCollection();
        svc.AddDbContext<HostelDbContext>(o => o.UseNpgsql(fx.PostgresConnection));
        var scopeFactory = svc.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

        var producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = fx.KafkaBootstrap,
            SecurityProtocol = SecurityProtocol.Plaintext
        }).Build();

        var relay = new HostelOutboxRelayService(
            scopeFactory,
            producer,
            NullLogger<HostelOutboxRelayService>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await relay.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromSeconds(8), cts.Token);
        await relay.StopAsync(cts.Token);
        producer.Dispose();

        var consumed = await fx.ConsumeOneAsync("hostel-events", "test-hostel", TimeSpan.FromSeconds(10));
        consumed.Should().NotBeNull();
        consumed!.Message.Key.Should().Be(msg.Id.ToString());

        await using var verifyDb = BuildDb();
        var stored = await verifyDb.OutboxMessages.FindAsync(msg.Id);
        stored!.ProcessedAt.Should().NotBeNull();
    }
}
