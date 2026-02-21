using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Notification.Application.Events;
using Notification.Application.Services;
using Notification.Domain.Enums;
namespace Notification.Infrastructure.Kafka.Consumers;
public sealed class FeeEventsConsumer : KafkaConsumerBase<FeePaymentReceivedEvent>
{
    public FeeEventsConsumer(IServiceScopeFactory scopeFactory, ILogger<FeeEventsConsumer> logger)
        : base(scopeFactory, logger, "fee-events", "notification-fee-group") { }
    protected override async Task ProcessAsync(FeePaymentReceivedEvent e, IServiceProvider services, CancellationToken ct)
    {
        var dispatcher = services.GetRequiredService<NotificationDispatcher>();
        await dispatcher.DispatchAsync(
            e.TenantId, e.StudentId, $"student-{e.StudentId}@ums.edu",
            "FeePaymentReceivedEvent",
            new Dictionary<string, string>
            {
                { "AmountPaid", e.AmountPaid.ToString("F2") },
                { "PaymentId", e.PaymentId.ToString() }
            },
            NotificationChannel.Email, ct);
    }
}
