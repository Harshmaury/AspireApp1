using Microsoft.Extensions.Logging;
using Notification.Application.Interfaces;
namespace Notification.Infrastructure.Channels;
// Stub — Twilio-ready for Sprint 11
public sealed class SmsChannel : INotificationChannel
{
    private readonly ILogger<SmsChannel> _logger;
    public SmsChannel(ILogger<SmsChannel> logger) => _logger = logger;
    public Task<bool> SendAsync(string recipientAddress, string subject, string body, CancellationToken ct = default)
    {
        _logger.LogInformation("SMS stub: would send to {Recipient}: {Body}", recipientAddress, body);
        return Task.FromResult(true);
    }
}
