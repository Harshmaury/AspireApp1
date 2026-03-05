using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Notification.Application.Events;
using Notification.Application.Services;
using Notification.Domain.Enums;
using UMS.SharedKernel.Kafka;

namespace Notification.Infrastructure.Kafka.Consumers;

public sealed class AcademicEventsConsumer : KafkaConsumerBase<AcademicCalendarPublishedEvent>
{
    public AcademicEventsConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<AcademicEventsConsumer> logger,
        IConfiguration configuration)
        : base(scopeFactory, logger, KafkaTopics.AcademicEvents, "notification-api", "academic-events", configuration) { }

    protected override async Task ProcessAsync(
        AcademicCalendarPublishedEvent e, IServiceProvider services, CancellationToken ct)
    {
        var log = services.GetRequiredService<ILogger<AcademicEventsConsumer>>();
        log.LogWarning(
            "AcademicCalendarPublishedEvent for TenantId={TenantId} Year={Year} Semester={Semester} " +
            "requires broadcast recipient list - notification skipped. " +
            "Implement fan-out or recipient-query client in Phase 2.",
            e.TenantId, e.AcademicYear, e.Semester);
        await Task.CompletedTask;
    }
}