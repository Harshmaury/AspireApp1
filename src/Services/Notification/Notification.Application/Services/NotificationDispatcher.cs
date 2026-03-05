using Microsoft.Extensions.Logging;
using Notification.Application.Interfaces;
using Notification.Domain.Entities;
using Notification.Domain.Enums;

namespace Notification.Application.Services;

public sealed class NotificationDispatcher
{
    private readonly INotificationTemplateRepository   _templateRepo;
    private readonly INotificationLogRepository        _logRepo;
    private readonly INotificationPreferenceRepository _preferenceRepo;
    private readonly IEnumerable<INotificationChannel> _channels;
    private readonly ILogger<NotificationDispatcher>   _logger;

    public NotificationDispatcher(
        INotificationTemplateRepository   templateRepo,
        INotificationLogRepository        logRepo,
        INotificationPreferenceRepository preferenceRepo,
        IEnumerable<INotificationChannel> channels,
        ILogger<NotificationDispatcher>   logger)
    {
        _templateRepo   = templateRepo;
        _logRepo        = logRepo;
        _preferenceRepo = preferenceRepo;
        _channels       = channels;
        _logger         = logger;
    }

    public async Task DispatchAsync(
        Guid   tenantId,
        Guid   recipientId,
        string recipientEmail,
        string eventType,
        Dictionary<string, string> templateData,
        NotificationChannel channel = NotificationChannel.Email,
        CancellationToken   ct      = default)
    {
        try
        {
            // Respect user channel preferences
            var preference = await _preferenceRepo.GetByUserAsync(recipientId, tenantId, ct);
            if (preference is not null)
            {
                if (channel == NotificationChannel.Email && !preference.EmailEnabled)
                {
                    _logger.LogInformation("Email notifications disabled for user {UserId}", recipientId);
                    return;
                }
                if (channel == NotificationChannel.SMS && !preference.SmsEnabled)
                {
                    _logger.LogInformation("SMS notifications disabled for user {UserId}", recipientId);
                    return;
                }
                if (channel == NotificationChannel.Push && !preference.PushEnabled)
                {
                    _logger.LogInformation("Push notifications disabled for user {UserId}", recipientId);
                    return;
                }
            }

            // Resolve template - tenant-specific first, then global default
            var template =
                await _templateRepo.GetByEventTypeAsync(tenantId, eventType, channel, ct)
             ?? await _templateRepo.GetDefaultAsync(eventType, channel, ct);

            if (template is null)
            {
                _logger.LogWarning("No template for event {EventType} channel {Channel}", eventType, channel);
                return;
            }

            var subject = template.RenderSubject(templateData);
            var body    = template.RenderBody(templateData);

            var log = NotificationLog.Create(tenantId, recipientId, recipientEmail, eventType, channel, subject, body);
            await _logRepo.AddAsync(log, ct);

            // Match by ChannelType property - not by class name convention.
            // Name-based matching breaks on Moq mocks in tests and breaks silently
            // if an implementation class is ever renamed.
            var channelImpl = _channels.FirstOrDefault(c => c.ChannelType == channel);

            if (channelImpl is null)
            {
                _logger.LogWarning("No channel implementation registered for {Channel}", channel);
                return;
            }

            try
            {
                var sent = await channelImpl.SendAsync(recipientEmail, subject, body, ct);
                if (sent)
                {
                    log.MarkSent();
                    await _logRepo.UpdateAsync(log, ct);
                    _logger.LogInformation("Notification sent to {Email} for event {EventType}", recipientEmail, eventType);
                }
                else
                {
                    log.MarkFailed("Channel returned false");
                    await _logRepo.UpdateAsync(log, ct);
                    _logger.LogWarning("Notification send returned false for {Email} - queued for retry", recipientEmail);
                }
            }
            catch (Exception ex)
            {
                log.MarkFailed(ex.Message);
                await _logRepo.UpdateAsync(log, ct);
                _logger.LogWarning(ex, "Notification send failed for {Email} - queued for retry", recipientEmail);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NotificationDispatcher unhandled error for event {EventType}", eventType);
        }
    }

    // Called by NotificationRetryBackgroundService
    public async Task RetryAsync(NotificationLog log, CancellationToken ct = default)
    {
        if (!log.CanRetry()) return;

        var channelImpl = _channels.FirstOrDefault(c => c.ChannelType == log.Channel);

        if (channelImpl is null)
        {
            _logger.LogWarning("Retry: no channel impl for {Channel}", log.Channel);
            return;
        }

        try
        {
            var sent = await channelImpl.SendAsync(log.RecipientAddress, log.Subject, log.Body, ct);
            if (sent)
            {
                log.MarkSent();
                _logger.LogInformation("Retry succeeded for log {LogId}", log.Id);
            }
            else
            {
                log.MarkFailed("Channel returned false on retry");
                _logger.LogWarning("Retry returned false for log {LogId}", log.Id);
            }
        }
        catch (Exception ex)
        {
            log.MarkFailed(ex.Message);
            _logger.LogWarning(ex, "Retry failed for log {LogId}", log.Id);
        }

        await _logRepo.UpdateAsync(log, ct);
    }
}