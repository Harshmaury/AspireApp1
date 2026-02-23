using Examination.Domain.Common;
using Examination.Infrastructure.Kafka;
using Examination.Infrastructure.Persistence;
using FluentAssertions;
using Kafka.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kafka.IntegrationTests.Tests;

[Collection("KafkaPostgres")]
public sealed class ExaminationOutboxRelayTests(KafkaPostgresFixture fx)
{
    private ExaminationDbContext BuildDb()
    {
        var opts = new DbContextOptionsBuilder<ExaminationDbContext>()
            .UseNpgsql(fx.PostgresConnection).Options;
        var db = new ExaminationDbContext(opts);
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public async Task Relay_publishes_outbox_message_to_examination_events_topic()
    {
        await using var db = BuildDb();
        var msg = new OutboxMessage { EventType = "Examination.ResultPublished", Payload = """{"examId":"ex-1"}""" };
        db.OutboxMessages.Add(msg);
        await db.SaveChangesAsync();

        var svc = new ServiceCollection();
        svc.AddDbContext<ExaminationDbContext>(o => o.UseNpgsql(fx.PostgresConnection));
        var scopeFactory = svc.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

        var relay = new ExaminationOutboxRelayService(
            scopeFactory,
            NullLogger<ExaminationOutboxRelayService>.Instance,
            fx.Configuration);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await relay.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromSeconds(8), cts.Token);
        await relay.StopAsync(cts.Token);

        var consumed = await fx.ConsumeOneAsync("examination-events", "test-examination", TimeSpan.FromSeconds(10));
        consumed.Should().NotBeNull();
        consumed!.Message.Key.Should().Be(msg.Id.ToString());

        await using var verifyDb = BuildDb();
        var stored = await verifyDb.OutboxMessages.FindAsync(msg.Id);
        stored!.ProcessedAt.Should().NotBeNull();
    }
}

