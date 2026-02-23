using Academic.Domain.Common;
using Academic.Infrastructure.Kafka;
using Academic.Infrastructure.Persistence;
using FluentAssertions;
using Kafka.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kafka.IntegrationTests.Tests;

[Collection("KafkaPostgres")]
public sealed class AcademicOutboxRelayTests(KafkaPostgresFixture fx)
{
    private AcademicDbContext BuildDb()
    {
        var opts = new DbContextOptionsBuilder<AcademicDbContext>()
            .UseNpgsql(fx.PostgresConnection)
            .Options;
        var db = new AcademicDbContext(opts);
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public async Task Relay_publishes_outbox_message_to_academic_events_topic()
    {
        // Arrange
        await using var db = BuildDb();
        var msg = OutboxMessage.Create("Academic.CourseCreated", """{"courseId":"abc"}""");
        db.OutboxMessages.Add(msg);
        await db.SaveChangesAsync();

        var relay = new AcademicOutboxRelayService(
            BuildServiceScopeFactory(),
            NullLogger<AcademicOutboxRelayService>.Instance,
            fx.Configuration);

        // Act — run one relay cycle
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await relay.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromSeconds(8), cts.Token);
        await relay.StopAsync(cts.Token);

        // Assert — message arrived on Kafka
        var consumed = await fx.ConsumeOneAsync("academic-events", "test-academic", TimeSpan.FromSeconds(10));
        consumed.Should().NotBeNull();
        consumed!.Message.Key.Should().Be(msg.Id.ToString());
        consumed!.Message.Value.Should().Contain("courseId");

        // Assert — DB marked processed
        await using var verifyDb = BuildDb();
        var stored = await verifyDb.OutboxMessages.FindAsync(msg.Id);
        stored!.ProcessedAt.Should().NotBeNull();
    }

    private IServiceScopeFactory BuildServiceScopeFactory()
    {
        var svc = new ServiceCollection();
        svc.AddDbContext<AcademicDbContext>(o => o.UseNpgsql(fx.PostgresConnection));
        return svc.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }
}

