using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Notification.Application.Events;
using Notification.Application.Services;
using Notification.Domain.Enums;
namespace Notification.Infrastructure.Kafka.Consumers;
public sealed class ExaminationEventsConsumer : KafkaConsumerBase<ResultDeclaredEvent>
{
    public ExaminationEventsConsumer(IServiceScopeFactory scopeFactory, ILogger<ExaminationEventsConsumer> logger)
        : base(scopeFactory, logger, "examination-events", "notification-examination-group") { }
    protected override async Task ProcessAsync(ResultDeclaredEvent e, IServiceProvider services, CancellationToken ct)
    {
        var dispatcher = services.GetRequiredService<NotificationDispatcher>();
        await dispatcher.DispatchAsync(
            e.TenantId, e.StudentId, $"student-{e.StudentId}@ums.edu",
            "ResultDeclaredEvent",
            new Dictionary<string, string>
            {
                { "AcademicYear", e.AcademicYear },
                { "Semester", e.Semester.ToString() },
                { "SGPA", e.SGPA.ToString("F2") },
                { "CGPA", e.CGPA.ToString("F2") }
            },
            NotificationChannel.Email, ct);
    }
}
