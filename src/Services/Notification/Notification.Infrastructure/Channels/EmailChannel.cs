using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Notification.Application.Interfaces;
namespace Notification.Infrastructure.Channels;
public sealed class EmailChannel : INotificationChannel
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailChannel> _logger;
    public EmailChannel(IConfiguration configuration, ILogger<EmailChannel> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }
    public async Task<bool> SendAsync(string recipientAddress, string subject, string body, CancellationToken ct = default)
    {
        try
        {
            var host = _configuration["Email:Host"] ?? "localhost";
            var port = int.Parse(_configuration["Email:Port"] ?? "587");
            var username = _configuration["Email:Username"] ?? "";
            var password = _configuration["Email:Password"] ?? "";
            var from = _configuration["Email:From"] ?? "noreply@ums.edu";
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(from));
            message.To.Add(MailboxAddress.Parse(recipientAddress));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = body };
            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, SecureSocketOptions.StartTlsWhenAvailable, ct);
            if (!string.IsNullOrEmpty(username))
                await client.AuthenticateAsync(username, password, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email send failed to {Recipient}", recipientAddress);
            return false;
        }
    }
}
