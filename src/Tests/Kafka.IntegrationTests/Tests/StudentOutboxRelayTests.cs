using FluentAssertions;
using Kafka.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Student.Domain.Common;
using Student.API.Services;
using Student.Infrastructure.Persistence;

namespace Kafka.IntegrationTests.Tests;

[Collection("KafkaPostgres")]
public sealed class StudentOutboxRelayTests(KafkaPostgresFixture fx)
{
    private StudentDbContext BuildDb()
    {
        var opts = new DbContextOptionsBuilder<StudentDbContext>()
            .UseNpgsql(fx.PostgresConnection).Options;
        var db = new StudentDbContext(opts);
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public async Task Relay_publishes_outbox_message_to_student_events_topic()
    {
        await using var db = BuildDb();
        var msg = OutboxMessage.Create("Student.StudentEnrolled", """{"studentId":"xyz"}""");
        db.OutboxMessages.Add(msg);
        await db.SaveChangesAsync();

        var svc = new ServiceCollection();
        svc.AddDbContext<StudentDbContext>(o => o.UseNpgsql(fx.PostgresConnection));
        var scopeFactory = svc.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

        var relay = new StudentOutboxRelayService(
            scopeFactory,
            NullLogger<StudentOutboxRelayService>.Instance,
            fx.Configuration);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await relay.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromSeconds(8), cts.Token);
        await relay.StopAsync(cts.Token);

        var consumed = await fx.ConsumeOneAsync("student-events", "test-student", TimeSpan.FromSeconds(10));
        consumed.Should().NotBeNull();
        consumed!.Message.Key.Should().Be(msg.Id.ToString());

        await using var verifyDb = BuildDb();
        var stored = await verifyDb.OutboxMessages.FindAsync(msg.Id);
        stored!.ProcessedAt.Should().NotBeNull();
    }
}

