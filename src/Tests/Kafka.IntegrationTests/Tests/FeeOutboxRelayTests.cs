using Fee.Domain.Common;
using Fee.Infrastructure.Kafka;
using Fee.Infrastructure.Persistence;
using FluentAssertions;
using Kafka.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kafka.IntegrationTests.Tests;

[Collection("KafkaPostgres")]
public sealed class FeeOutboxRelayTests(KafkaPostgresFixture fx)
{
    private FeeDbContext BuildDb()
    {
        var opts = new DbContextOptionsBuilder<FeeDbContext>()
            .UseNpgsql(fx.PostgresConnection).Options;
        var db = new FeeDbContext(opts);
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public async Task Relay_publishes_outbox_message_to_fee_events_topic()
    {
        await using var db = BuildDb();
        var msg = new OutboxMessage { EventType = "Fee.PaymentReceived", Payload = """{"feeId":"fee-1"}""" };
        db.OutboxMessages.Add(msg);
        await db.SaveChangesAsync();

        var svc = new ServiceCollection();
        svc.AddDbContext<FeeDbContext>(o => o.UseNpgsql(fx.PostgresConnection));
        var scopeFactory = svc.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

        var relay = new FeeOutboxRelayService(
            scopeFactory,
            NullLogger<FeeOutboxRelayService>.Instance,
            fx.Configuration);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await relay.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromSeconds(8), cts.Token);
        await relay.StopAsync(cts.Token);

        var consumed = await fx.ConsumeOneAsync("fee-events", "test-fee", TimeSpan.FromSeconds(10));
        consumed.Should().NotBeNull();
        consumed!.Message.Key.Should().Be(msg.Id.ToString());

        await using var verifyDb = BuildDb();
        var stored = await verifyDb.OutboxMessages.FindAsync(msg.Id);
        stored!.ProcessedAt.Should().NotBeNull();
    }
}

