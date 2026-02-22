using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Notification.Application.Interfaces;
using Notification.Application.Services;
using Notification.Domain.Entities;
using Notification.Domain.Enums;
using Xunit;

namespace Notification.Tests.Application;

public sealed class NotificationDispatcherTests
{
    private readonly Mock<INotificationTemplateRepository>   _templateRepo   = new();
    private readonly Mock<INotificationLogRepository>        _logRepo        = new();
    private readonly Mock<INotificationPreferenceRepository> _preferenceRepo = new();
    private readonly Mock<INotificationChannel>              _channel        = new();
    private readonly Mock<ILogger<NotificationDispatcher>>   _logger         = new();

    private NotificationDispatcher CreateDispatcher(bool registerChannel = true)
    {
        var channels = registerChannel
            ? new[] { _channel.Object }
            : Array.Empty<INotificationChannel>();
        return new NotificationDispatcher(
            _templateRepo.Object, _logRepo.Object, _preferenceRepo.Object,
            channels, _logger.Object);
    }

    private static NotificationTemplate MakeTemplate(string eventType = "TestEvent") =>
        NotificationTemplate.Create(Guid.Empty, eventType, NotificationChannel.Email,
            "Subject for {{Name}}", "<p>Hello {{Name}}</p>");

    private static readonly Dictionary<string, string> TemplateData =
        new() { { "Name", "Alice" } };

    [Fact]
    public async Task DispatchAsync_EmailPreferenceDisabled_DoesNotAddLog()
    {
        var tenantId = Guid.NewGuid();
        var userId   = Guid.NewGuid();
        var pref = NotificationPreference.Create(tenantId, userId,
            emailEnabled: false, smsEnabled: true, pushEnabled: false);
        _preferenceRepo
            .Setup(r => r.GetByUserAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pref);

        await CreateDispatcher().DispatchAsync(tenantId, userId, "u@uni.edu",
            "TestEvent", TemplateData, NotificationChannel.Email);

        _logRepo.Verify(r => r.AddAsync(It.IsAny<NotificationLog>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DispatchAsync_NoTemplateFound_DoesNotAddLog()
    {
        var tenantId = Guid.NewGuid();
        _preferenceRepo
            .Setup(r => r.GetByUserAsync(It.IsAny<Guid>(), tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationPreference?)null);
        _templateRepo
            .Setup(r => r.GetByEventTypeAsync(tenantId, "TestEvent", NotificationChannel.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationTemplate?)null);
        _templateRepo
            .Setup(r => r.GetDefaultAsync("TestEvent", NotificationChannel.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationTemplate?)null);

        await CreateDispatcher().DispatchAsync(tenantId, Guid.NewGuid(), "u@uni.edu",
            "TestEvent", TemplateData, NotificationChannel.Email);

        _logRepo.Verify(r => r.AddAsync(It.IsAny<NotificationLog>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DispatchAsync_SendSuccess_LogMarkedSent()
    {
        var tenantId = Guid.NewGuid();
        var userId   = Guid.NewGuid();
        _preferenceRepo
            .Setup(r => r.GetByUserAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationPreference?)null);
        _templateRepo
            .Setup(r => r.GetByEventTypeAsync(tenantId, "TestEvent", NotificationChannel.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeTemplate());

        NotificationLog? capturedLog = null;
        _logRepo
            .Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationLog, CancellationToken>((log, _) => capturedLog = log)
            .Returns(Task.CompletedTask);
        _logRepo
            .Setup(r => r.UpdateAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _channel
            .Setup(c => c.SendAsync("u@uni.edu", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await CreateDispatcher().DispatchAsync(tenantId, userId, "u@uni.edu",
            "TestEvent", TemplateData, NotificationChannel.Email);

        capturedLog.Should().NotBeNull();
        capturedLog!.Status.Should().Be(NotificationStatus.Sent);
        capturedLog.SentAt.Should().NotBeNull();
        _logRepo.Verify(r => r.UpdateAsync(It.IsAny<NotificationLog>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_NoChannel_LogStaysPending()
    {
        var tenantId = Guid.NewGuid();
        _preferenceRepo
            .Setup(r => r.GetByUserAsync(It.IsAny<Guid>(), tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationPreference?)null);
        _templateRepo
            .Setup(r => r.GetByEventTypeAsync(tenantId, "TestEvent", NotificationChannel.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeTemplate());

        NotificationLog? capturedLog = null;
        _logRepo
            .Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationLog, CancellationToken>((log, _) => capturedLog = log)
            .Returns(Task.CompletedTask);

        await CreateDispatcher(registerChannel: false).DispatchAsync(tenantId, Guid.NewGuid(), "u@uni.edu",
            "TestEvent", TemplateData, NotificationChannel.Email);

        capturedLog.Should().NotBeNull();
        capturedLog!.Status.Should().Be(NotificationStatus.Pending);
        _logRepo.Verify(r => r.UpdateAsync(It.IsAny<NotificationLog>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DispatchAsync_FallsBackToDefaultTemplate_WhenTenantTemplateNull()
    {
        var tenantId   = Guid.NewGuid();
        var defaultTpl = MakeTemplate("TestEvent");
        _preferenceRepo
            .Setup(r => r.GetByUserAsync(It.IsAny<Guid>(), tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationPreference?)null);
        _templateRepo
            .Setup(r => r.GetByEventTypeAsync(tenantId, "TestEvent", NotificationChannel.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationTemplate?)null);
        _templateRepo
            .Setup(r => r.GetDefaultAsync("TestEvent", NotificationChannel.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultTpl);
        _logRepo
            .Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _logRepo
            .Setup(r => r.UpdateAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _channel
            .Setup(c => c.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await CreateDispatcher().DispatchAsync(tenantId, Guid.NewGuid(), "u@uni.edu",
            "TestEvent", TemplateData, NotificationChannel.Email);

        _logRepo.Verify(r => r.AddAsync(It.IsAny<NotificationLog>(),
            It.IsAny<CancellationToken>()), Times.Once);
        _templateRepo.Verify(r => r.GetDefaultAsync("TestEvent", NotificationChannel.Email,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
