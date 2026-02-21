using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Notification.Application.Events;
using Notification.Application.Services;
using Notification.Domain.Enums;

namespace Notification.Infrastructure.Kafka.Consumers;

public sealed class AcademicEventsConsumer : KafkaConsumerBase<AcademicCalendarPublishedEvent>
{
    public AcademicEventsConsumer(IServiceScopeFactory scopeFactory, ILogger<AcademicEventsConsumer> logger, IConfiguration configuration)
        : base(scopeFactory, logger, "academic-events", "notification-academic-group", configuration) { }

    protected override async Task ProcessAsync(AcademicCalendarPublishedEvent e, IServiceProvider services, CancellationToken ct)
    {
        var dispatcher = services.GetRequiredService<NotificationDispatcher>();
        await dispatcher.DispatchAsync(
            e.TenantId, Guid.Empty, "broadcast@ums.edu",
            "AcademicCalendarPublishedEvent",
            new Dictionary<string, string>
            {
                { "AcademicYear", e.AcademicYear },
                { "Semester", e.Semester.ToString() },
                { "StartDate", e.StartDate.ToString("dd MMM yyyy") },
                { "EndDate", e.EndDate.ToString("dd MMM yyyy") }
            },
            NotificationChannel.Email, ct);
    }
}
