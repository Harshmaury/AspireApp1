using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Notification.Application.Events;
using Notification.Application.Services;
using Notification.Domain.Enums;
using UMS.SharedKernel.Kafka;

namespace Notification.Infrastructure.Kafka.Consumers;

public sealed class StudentEventsConsumer : KafkaConsumerBase<StudentStatusChangedEvent>
{
    public StudentEventsConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<StudentEventsConsumer> logger,
        IConfiguration configuration)
        : base(scopeFactory, logger, KafkaTopics.StudentEvents, "notification-api", "student-events", configuration) { }

    protected override async Task ProcessAsync(
        StudentStatusChangedEvent e, IServiceProvider services, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(e.Email))
        {
            var log = services.GetRequiredService<ILogger<StudentEventsConsumer>>();
            log.LogWarning(
                "StudentStatusChangedEvent missing Email for StudentId={StudentId} NewStatus={NewStatus} - skipping",
                e.StudentId, e.NewStatus);
            return;
        }

        var templateEvent = e.NewStatus switch
        {
            "Enrolled"  => "StudentEnrolledEvent",
            "Suspended" => "StudentSuspendedEvent",
            "Alumni"    => "StudentGraduatedEvent",
            _           => null
        };

        if (templateEvent is null) return;

        var dispatcher = services.GetRequiredService<NotificationDispatcher>();
        await dispatcher.DispatchAsync(
            e.TenantId, e.StudentId, e.Email,
            templateEvent,
            new Dictionary<string, string>
            {
                { "FirstName", e.FirstName ?? "" },
                { "OldStatus", e.OldStatus  ?? "" },
                { "NewStatus", e.NewStatus  ?? "" }
            },
            NotificationChannel.Email, ct);
    }
}