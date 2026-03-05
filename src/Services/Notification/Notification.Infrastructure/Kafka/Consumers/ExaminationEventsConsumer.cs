using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Notification.Application.Events;
using Notification.Application.Services;
using Notification.Domain.Enums;
using UMS.SharedKernel.Kafka;

namespace Notification.Infrastructure.Kafka.Consumers;

public sealed class ExaminationEventsConsumer : KafkaConsumerBase<ResultDeclaredEvent>
{
    public ExaminationEventsConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<ExaminationEventsConsumer> logger,
        IConfiguration configuration)
        : base(scopeFactory, logger, KafkaTopics.ExaminationEvents, "notification-api", "examination-events", configuration) { }

    protected override async Task ProcessAsync(
        ResultDeclaredEvent e, IServiceProvider services, CancellationToken ct)
    {
        var log = services.GetRequiredService<ILogger<ExaminationEventsConsumer>>();
        log.LogWarning(
            "ResultDeclaredEvent has no Email field - notification skipped for StudentId={StudentId}. " +
            "Add email enrichment in Phase 2.", e.StudentId);
        await Task.CompletedTask;
    }
}