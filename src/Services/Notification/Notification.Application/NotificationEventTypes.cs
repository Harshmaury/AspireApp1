// UMS — University Management System
// Key:     UMS-NOTIFICATION-P2-002
// Service: Notification
// Layer:   Application
namespace Notification.Application;

/// <summary>
/// Single source of truth for event type strings used in notification templates.
/// Must match the event class names published from Identity service via Kafka.
/// </summary>
public static class NotificationEventTypes
{
    // ── Identity events ───────────────────────────────────────────────────
    public const string UserRegistered              = "UserRegisteredEvent";
    public const string EmailVerificationRequested  = "EmailVerificationRequestedEvent";
    public const string PasswordResetRequested      = "PasswordResetRequestedEvent";
    public const string PasswordResetCompleted      = "PasswordResetCompletedEvent";
    public const string UserDeactivated             = "UserDeactivatedEvent";
    public const string UserLockedOut               = "UserLockedOutEvent";

    // ── Academic events ───────────────────────────────────────────────────
    public const string ExamScheduled               = "ExamScheduledEvent";
    public const string ResultPublished             = "ResultPublishedEvent";

    // ── Fee events ────────────────────────────────────────────────────────
    public const string FeePaymentDue               = "FeePaymentDueEvent";
    public const string FeePaymentReceived          = "FeePaymentReceivedEvent";

    // ── Attendance events ─────────────────────────────────────────────────
    public const string LowAttendanceWarning        = "LowAttendanceWarningEvent";
}
