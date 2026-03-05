using Notification.Domain.Enums;

namespace Notification.Application.Interfaces;

public interface INotificationChannel
{
    // Explicit channel type declaration.
    // The dispatcher matches on this property instead of class name convention.
    // Class name matching (c.GetType().Name.StartsWith("Email")) breaks on Moq
    // mocks in tests and breaks silently if a class is ever renamed.
    NotificationChannel ChannelType { get; }

    Task<bool> SendAsync(string recipientAddress, string subject, string body, CancellationToken ct = default);
}