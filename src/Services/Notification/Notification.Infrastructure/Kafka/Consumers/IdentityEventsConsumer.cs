// UMS — University Management System
// Key:     UMS-NOTIFICATION-P2-001
// Service: Notification
// Layer:   Infrastructure / Kafka / Consumers
namespace Notification.Infrastructure.Kafka.Consumers;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Notification.Application.Events;
using Notification.Application.Services;
using Notification.Domain.Enums;
using UMS.SharedKernel.Kafka;
using Notification.Application;

// ── UserRegistered ────────────────────────────────────────────────────────

public sealed class IdentityEventsConsumer
    : KafkaConsumerBase<UserRegisteredEvent>
{
    public IdentityEventsConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<IdentityEventsConsumer> logger,
        IConfiguration configuration)
        : base(scopeFactory, logger,
               KafkaTopics.IdentityEvents,
               "notification-api", "identity-events", configuration) { }

    protected override async Task ProcessAsync(
        UserRegisteredEvent e, IServiceProvider services, CancellationToken ct)
    {
        var dispatcher = services.GetRequiredService<NotificationDispatcher>();
        await dispatcher.DispatchAsync(
            e.TenantId, e.UserId, e.Email,
            NotificationEventTypes.UserRegistered,
            new Dictionary<string, string>
            {
                { "FirstName", e.FirstName },
                { "LastName",  e.LastName  },
                { "Email",     e.Email     },
                { "Role",      e.Role      }
            },
            NotificationChannel.Email, ct);
    }
}

// ── EmailVerificationRequested ────────────────────────────────────────────

public sealed class EmailVerificationRequestedConsumer
    : KafkaConsumerBase<EmailVerificationRequestedEvent>
{
    public EmailVerificationRequestedConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<EmailVerificationRequestedConsumer> logger,
        IConfiguration configuration)
        : base(scopeFactory, logger,
               KafkaTopics.IdentityEvents,
               "notification-api", "identity-email-verification", configuration) { }

    protected override async Task ProcessAsync(
        EmailVerificationRequestedEvent e,
        IServiceProvider services,
        CancellationToken ct)
    {
        var dispatcher = services.GetRequiredService<NotificationDispatcher>();
        await dispatcher.DispatchAsync(
            e.TenantId, e.UserId, e.Email,
            NotificationEventTypes.EmailVerificationRequested,
            new Dictionary<string, string>
            {
                { "Email",           e.Email          },
                { "VerificationUrl", e.VerificationUrl },
                { "ExpiresInHours",  "24"             }
            },
            NotificationChannel.Email, ct);
    }
}

// ── PasswordResetRequested ────────────────────────────────────────────────

public sealed class PasswordResetRequestedConsumer
    : KafkaConsumerBase<PasswordResetRequestedEvent>
{
    public PasswordResetRequestedConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<PasswordResetRequestedConsumer> logger,
        IConfiguration configuration)
        : base(scopeFactory, logger,
               KafkaTopics.IdentityEvents,
               "notification-api", "identity-password-reset", configuration) { }

    protected override async Task ProcessAsync(
        PasswordResetRequestedEvent e,
        IServiceProvider services,
        CancellationToken ct)
    {
        var dispatcher = services.GetRequiredService<NotificationDispatcher>();
        await dispatcher.DispatchAsync(
            e.TenantId, e.UserId, e.Email,
            NotificationEventTypes.PasswordResetRequested,
            new Dictionary<string, string>
            {
                { "Email",        e.Email      },
                { "ResetUrl",     e.ResetUrl   },
                { "ExpiresInHours", "1"        }
            },
            NotificationChannel.Email, ct);
    }
}

// ── PasswordResetCompleted ────────────────────────────────────────────────

public sealed class PasswordResetCompletedConsumer
    : KafkaConsumerBase<PasswordResetCompletedEvent>
{
    public PasswordResetCompletedConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<PasswordResetCompletedConsumer> logger,
        IConfiguration configuration)
        : base(scopeFactory, logger,
               KafkaTopics.IdentityEvents,
               "notification-api", "identity-password-reset-completed", configuration) { }

    protected override async Task ProcessAsync(
        PasswordResetCompletedEvent e,
        IServiceProvider services,
        CancellationToken ct)
    {
        var dispatcher = services.GetRequiredService<NotificationDispatcher>();
        await dispatcher.DispatchAsync(
            e.TenantId, e.UserId, e.Email,
            NotificationEventTypes.PasswordResetCompleted,
            new Dictionary<string, string>
            {
                { "Email",     e.Email                               },
                { "OccuredAt", e.OccurredAt.ToString("f")           }
            },
            NotificationChannel.Email, ct);
    }
}
