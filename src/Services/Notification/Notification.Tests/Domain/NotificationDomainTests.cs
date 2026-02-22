using FluentAssertions;
using Notification.Domain.Entities;
using Notification.Domain.Enums;
using Notification.Domain.Exceptions;
using Xunit;

namespace Notification.Tests.Domain;

public sealed class NotificationLogTests
{
    private static readonly Guid TenantId    = Guid.NewGuid();
    private static readonly Guid RecipientId = Guid.NewGuid();

    [Fact]
    public void Create_ValidArgs_ReturnsPendingLog()
    {
        var log = NotificationLog.Create(TenantId, RecipientId, "user@uni.edu",
            "UserRegisteredEvent", NotificationChannel.Email, "Welcome!", "<p>Hi</p>");
        log.TenantId.Should().Be(TenantId);
        log.RecipientId.Should().Be(RecipientId);
        log.RecipientAddress.Should().Be("user@uni.edu");
        log.Status.Should().Be(NotificationStatus.Pending);
        log.RetryCount.Should().Be(0);
        log.SentAt.Should().BeNull();
        log.Id.Should().NotBe(Guid.Empty);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyAddress_Throws(string address)
    {
        var ex = Assert.Throws<NotificationDomainException>(() =>
            NotificationLog.Create(TenantId, RecipientId, address,
                "Event", NotificationChannel.Email, "Subj", "Body"));
        ex.Code.Should().Be("INVALID_ADDRESS");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptySubject_Throws(string subject)
    {
        var ex = Assert.Throws<NotificationDomainException>(() =>
            NotificationLog.Create(TenantId, RecipientId, "a@b.com",
                "Event", NotificationChannel.Email, subject, "Body"));
        ex.Code.Should().Be("INVALID_SUBJECT");
    }

    [Fact]
    public void MarkSent_SetsSentStatusAndSentAt()
    {
        var log = MakeLog();
        log.MarkSent();
        log.Status.Should().Be(NotificationStatus.Sent);
        log.SentAt.Should().NotBeNull();
        log.SentAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void MarkFailed_FirstCall_SetsFailedAndIncrementsRetry()
    {
        var log = MakeLog();
        log.MarkFailed("SMTP timeout");
        log.Status.Should().Be(NotificationStatus.Failed);
        log.RetryCount.Should().Be(1);
        log.ErrorMessage.Should().Be("SMTP timeout");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void MarkFailed_EmptyMessage_Throws(string msg)
    {
        var log = MakeLog();
        var ex = Assert.Throws<NotificationDomainException>(() => log.MarkFailed(msg));
        ex.Code.Should().Be("INVALID_ERROR");
    }

    [Fact]
    public void MarkFailed_FiveTimes_SetsDeadLettered()
    {
        var log = MakeLog();
        for (var i = 0; i < 5; i++) log.MarkFailed("err");
        log.Status.Should().Be(NotificationStatus.DeadLettered);
        log.RetryCount.Should().Be(5);
    }

    [Fact]
    public void CanRetry_WhenFailed_AndUnderLimit_ReturnsTrue()
    {
        var log = MakeLog();
        log.MarkFailed("err");
        log.CanRetry().Should().BeTrue();
    }

    [Fact]
    public void CanRetry_WhenDeadLettered_ReturnsFalse()
    {
        var log = MakeLog();
        for (var i = 0; i < 5; i++) log.MarkFailed("err");
        log.CanRetry().Should().BeFalse();
    }

    [Fact]
    public void CanRetry_WhenSent_ReturnsFalse()
    {
        var log = MakeLog();
        log.MarkSent();
        log.CanRetry().Should().BeFalse();
    }

    private static NotificationLog MakeLog() =>
        NotificationLog.Create(TenantId, Guid.NewGuid(), "x@y.com",
            "TestEvent", NotificationChannel.Email, "Subject", "Body");
}

public sealed class NotificationPreferenceTests
{
    [Fact]
    public void Create_ValidArgs_ReturnsPreferenceWithDefaults()
    {
        var tenantId = Guid.NewGuid();
        var userId   = Guid.NewGuid();
        var pref = NotificationPreference.Create(tenantId, userId);
        pref.TenantId.Should().Be(tenantId);
        pref.UserId.Should().Be(userId);
        pref.EmailEnabled.Should().BeTrue();
        pref.SmsEnabled.Should().BeTrue();
        pref.PushEnabled.Should().BeFalse();
        pref.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_EmptyTenantId_Throws()
    {
        var ex = Assert.Throws<NotificationDomainException>(() =>
            NotificationPreference.Create(Guid.Empty, Guid.NewGuid()));
        ex.Code.Should().Be("INVALID_TENANT");
    }

    [Fact]
    public void Create_EmptyUserId_Throws()
    {
        var ex = Assert.Throws<NotificationDomainException>(() =>
            NotificationPreference.Create(Guid.NewGuid(), Guid.Empty));
        ex.Code.Should().Be("INVALID_USER");
    }

    [Fact]
    public void Create_CustomFlags_StoredCorrectly()
    {
        var pref = NotificationPreference.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            emailEnabled: false, smsEnabled: false, pushEnabled: true);
        pref.EmailEnabled.Should().BeFalse();
        pref.SmsEnabled.Should().BeFalse();
        pref.PushEnabled.Should().BeTrue();
    }

    [Fact]
    public void Update_ChangesAllFlags_AndSetsUpdatedAt()
    {
        var pref = NotificationPreference.Create(Guid.NewGuid(), Guid.NewGuid());
        pref.Update(emailEnabled: false, smsEnabled: false, pushEnabled: true);
        pref.EmailEnabled.Should().BeFalse();
        pref.SmsEnabled.Should().BeFalse();
        pref.PushEnabled.Should().BeTrue();
        pref.UpdatedAt.Should().NotBeNull();
    }
}

public sealed class NotificationTemplateTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public void Create_ValidArgs_ReturnsActiveTemplate()
    {
        var t = MakeTemplate();
        t.TenantId.Should().Be(TenantId);
        t.EventType.Should().Be("UserRegisteredEvent");
        t.Channel.Should().Be(NotificationChannel.Email);
        t.IsActive.Should().BeTrue();
        t.Id.Should().NotBe(Guid.Empty);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyEventType_Throws(string eventType)
    {
        var ex = Assert.Throws<NotificationDomainException>(() =>
            NotificationTemplate.Create(TenantId, eventType,
                NotificationChannel.Email, "Subject", "Body"));
        ex.Code.Should().Be("INVALID_EVENT_TYPE");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptySubjectTemplate_Throws(string subject)
    {
        var ex = Assert.Throws<NotificationDomainException>(() =>
            NotificationTemplate.Create(TenantId, "Event",
                NotificationChannel.Email, subject, "Body"));
        ex.Code.Should().Be("INVALID_SUBJECT");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyBodyTemplate_Throws(string body)
    {
        var ex = Assert.Throws<NotificationDomainException>(() =>
            NotificationTemplate.Create(TenantId, "Event",
                NotificationChannel.Email, "Subject", body));
        ex.Code.Should().Be("INVALID_BODY");
    }

    [Fact]
    public void Update_ValidArgs_UpdatesTemplatesAndSetsUpdatedAt()
    {
        var t = MakeTemplate();
        t.Update("New Subject", "<p>New Body</p>");
        t.SubjectTemplate.Should().Be("New Subject");
        t.BodyTemplate.Should().Be("<p>New Body</p>");
        t.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Update_EmptySubject_Throws()
    {
        var t = MakeTemplate();
        var ex = Assert.Throws<NotificationDomainException>(() => t.Update("", "body"));
        ex.Code.Should().Be("INVALID_SUBJECT");
    }

    [Fact]
    public void Deactivate_ActiveTemplate_SetsIsActiveFalse()
    {
        var t = MakeTemplate();
        t.Deactivate();
        t.IsActive.Should().BeFalse();
        t.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Deactivate_AlreadyInactive_Throws()
    {
        var t = MakeTemplate();
        t.Deactivate();
        var ex = Assert.Throws<NotificationDomainException>(() => t.Deactivate());
        ex.Code.Should().Be("ALREADY_INACTIVE");
    }

    [Fact]
    public void RenderSubject_ReplacesPlaceholders()
    {
        var t = NotificationTemplate.Create(TenantId, "Ev", NotificationChannel.Email,
            "Welcome {{FirstName}}!", "body");
        var result = t.RenderSubject(new Dictionary<string, string> { { "FirstName", "Alice" } });
        result.Should().Be("Welcome Alice!");
    }

    [Fact]
    public void RenderBody_ReplacesMultiplePlaceholders()
    {
        var t = NotificationTemplate.Create(TenantId, "Ev", NotificationChannel.Email,
            "subj", "SGPA: {{SGPA}} CGPA: {{CGPA}}");
        var result = t.RenderBody(new Dictionary<string, string>
        {
            { "SGPA", "8.50" },
            { "CGPA", "8.30" }
        });
        result.Should().Be("SGPA: 8.50 CGPA: 8.30");
    }

    private static NotificationTemplate MakeTemplate() =>
        NotificationTemplate.Create(TenantId, "UserRegisteredEvent",
            NotificationChannel.Email, "Welcome {{FirstName}}!", "<p>Hi {{FirstName}}</p>");
}
