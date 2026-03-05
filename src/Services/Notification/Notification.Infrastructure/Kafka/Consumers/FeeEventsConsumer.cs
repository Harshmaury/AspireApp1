using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Notification.Application.Events;
using Notification.Application.Services;
using Notification.Domain.Enums;
using UMS.SharedKernel.Kafka;

namespace Notification.Infrastructure.Kafka.Consumers;

public sealed class FeeEventsConsumer : KafkaConsumerBase<FeePaymentReceivedEvent>
{
    public FeeEventsConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<FeeEventsConsumer> logger,
        IConfiguration configuration)
        : base(scopeFactory, logger, KafkaTopics.FeeEvents, "notification-api", "fee-events", configuration) { }

    protected override async Task ProcessAsync(
        FeePaymentReceivedEvent e, IServiceProvider services, CancellationToken ct)
    {
        var log = services.GetRequiredService<ILogger<FeeEventsConsumer>>();
        log.LogWarning(
            "FeePaymentReceivedEvent has no Email field - notification skipped for StudentId={StudentId}. " +
            "Add email enrichment in Phase 2.", e.StudentId);
        await Task.CompletedTask;
    }
}