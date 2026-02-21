namespace Notification.Application.Interfaces;
public interface INotificationChannel
{
    Task<bool> SendAsync(string recipientAddress, string subject, string body, CancellationToken ct = default);
}
