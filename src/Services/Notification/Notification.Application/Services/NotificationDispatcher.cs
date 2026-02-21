using Microsoft.Extensions.Logging;
using Notification.Application.Interfaces;
using Notification.Domain.Entities;
using Notification.Domain.Enums;
namespace Notification.Application.Services;
public sealed class NotificationDispatcher
{
    private readonly INotificationTemplateRepository _templateRepository;
    private readonly INotificationLogRepository _logRepository;
    private readonly INotificationPreferenceRepository _preferenceRepository;
    private readonly IEnumerable<INotificationChannel> _channels;
    private readonly ILogger<NotificationDispatcher> _logger;

    public NotificationDispatcher(
        INotificationTemplateRepository templateRepository,
        INotificationLogRepository logRepository,
        INotificationPreferenceRepository preferenceRepository,
        IEnumerable<INotificationChannel> channels,
        ILogger<NotificationDispatcher> logger)
    {
        _templateRepository = templateRepository;
        _logRepository = logRepository;
        _preferenceRepository = preferenceRepository;
        _channels = channels;
        _logger = logger;
    }

    public async Task DispatchAsync(
        Guid tenantId,
        Guid recipientId,
        string recipientEmail,
        string eventType,
        Dictionary<string, string> templateData,
        NotificationChannel channel = NotificationChannel.Email,
        CancellationToken ct = default)
    {
        try
        {
            // Check preference
            var preference = await _preferenceRepository.GetByUserAsync(recipientId, tenantId, ct);
            if (preference is not null && channel == NotificationChannel.Email && !preference.EmailEnabled)
            {
                _logger.LogInformation("Email notifications disabled for user {UserId}", recipientId);
                return;
            }

            // Resolve template — tenant-specific first, fallback to default
            var template = await _templateRepository.GetByEventTypeAsync(tenantId, eventType, channel, ct)
                        ?? await _templateRepository.GetDefaultAsync(eventType, channel, ct);

            if (template is null)
            {
                _logger.LogWarning("No template found for event {EventType} channel {Channel}", eventType, channel);
                return;
            }

            var subject = template.RenderSubject(templateData);
            var body = template.RenderBody(templateData);

            var log = NotificationLog.Create(tenantId, recipientId, recipientEmail, eventType, channel, subject, body);
            await _logRepository.AddAsync(log, ct);

            // Retry logic with exponential backoff
            var delays = new[] { 0, 5, 15, 30, 60 };
            foreach (var delay in delays)
            {
                if (delay > 0) await Task.Delay(TimeSpan.FromSeconds(delay), ct);
                try
                {
                    var channelImpl = _channels.FirstOrDefault();
                    if (channelImpl is null) break;
                    var sent = await channelImpl.SendAsync(recipientEmail, subject, body, ct);
                    if (sent)
                    {
                        log.MarkSent();
                        await _logRepository.UpdateAsync(log, ct);
                        _logger.LogInformation("Notification sent to {Email} for event {EventType}", recipientEmail, eventType);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    log.MarkFailed(ex.Message);
                    await _logRepository.UpdateAsync(log, ct);
                    if (!log.CanRetry())
                    {
                        _logger.LogError("Notification dead-lettered for {Email} event {EventType}", recipientEmail, eventType);
                        return;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NotificationDispatcher failed for event {EventType}", eventType);
        }
    }
}
