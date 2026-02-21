using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Notification.Application.Events;
using Notification.Application.Services;
using Notification.Domain.Enums;

namespace Notification.Infrastructure.Kafka.Consumers;

public sealed class IdentityEventsConsumer : KafkaConsumerBase<UserRegisteredEvent>
{
    public IdentityEventsConsumer(IServiceScopeFactory scopeFactory, ILogger<IdentityEventsConsumer> logger, IConfiguration configuration)
        : base(scopeFactory, logger, "identity-events", "notification-identity-group", configuration) { }

    protected override async Task ProcessAsync(UserRegisteredEvent e, IServiceProvider services, CancellationToken ct)
    {
        var dispatcher = services.GetRequiredService<NotificationDispatcher>();
        await dispatcher.DispatchAsync(
            e.TenantId, e.UserId, e.Email,
            "UserRegisteredEvent",
            new Dictionary<string, string>
            {
                { "FirstName", e.FirstName },
                { "LastName", e.LastName },
                { "Email", e.Email },
                { "Role", e.Role }
            },
            NotificationChannel.Email, ct);
    }
}
