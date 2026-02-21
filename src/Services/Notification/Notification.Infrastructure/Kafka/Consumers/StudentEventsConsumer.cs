using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Notification.Application.Events;
using Notification.Application.Services;
using Notification.Domain.Enums;

namespace Notification.Infrastructure.Kafka.Consumers;

public sealed class StudentEventsConsumer : KafkaConsumerBase<StudentEnrolledEvent>
{
    public StudentEventsConsumer(IServiceScopeFactory scopeFactory, ILogger<StudentEventsConsumer> logger, IConfiguration configuration)
        : base(scopeFactory, logger, "student-events", "notification-student-group", configuration) { }

    protected override async Task ProcessAsync(StudentEnrolledEvent e, IServiceProvider services, CancellationToken ct)
    {
        var dispatcher = services.GetRequiredService<NotificationDispatcher>();
        await dispatcher.DispatchAsync(
            e.TenantId, e.StudentId, e.Email,
            "StudentEnrolledEvent",
            new Dictionary<string, string>
            {
                { "FirstName", e.FirstName },
                { "StudentNumber", e.StudentNumber }
            },
            NotificationChannel.Email, ct);
    }
}
