// UMS — University Management System
// Key:     UMS-NOTIFICATION-P2-005
// Service: Notification
// Layer:   Infrastructure / Templates
namespace Notification.Infrastructure.Templates;

using Notification.Application;
using Notification.Domain.Entities;
using Notification.Domain.Enums;

/// <summary>
/// Default email templates seeded on first startup.
/// Variables use {{VariableName}} syntax — matched by NotificationTemplate.Render().
/// </summary>
public static class DefaultTemplates
{
    private static readonly Guid GlobalTenantId = Guid.Empty;

    public static IEnumerable<NotificationTemplate> GetAll() =>
    [
        // ── Registration welcome ──────────────────────────────────────────
        NotificationTemplate.Create(
            tenantId:        GlobalTenantId,
            eventType:       NotificationEventTypes.UserRegistered,
            channel:         NotificationChannel.Email,
            subjectTemplate: "Welcome to UMS, {{FirstName}}!",
            bodyTemplate:    """
                <h2>Welcome, {{FirstName}} {{LastName}}!</h2>
                <p>Your account has been created successfully.</p>
                <p><strong>Email:</strong> {{Email}}</p>
                <p><strong>Role:</strong> {{Role}}</p>
                <p>Please verify your email address to activate your account.</p>
                <br/>
                <p>The UMS Team</p>
                """),

        // ── Email verification ────────────────────────────────────────────
        NotificationTemplate.Create(
            tenantId:        GlobalTenantId,
            eventType:       NotificationEventTypes.EmailVerificationRequested,
            channel:         NotificationChannel.Email,
            subjectTemplate: "Verify your UMS email address",
            bodyTemplate:    """
                <h2>Verify Your Email Address</h2>
                <p>Please click the button below to verify your email address.</p>
                <p>This link expires in <strong>{{ExpiresInHours}} hours</strong>.</p>
                <br/>
                <a href="{{VerificationUrl}}"
                   style="background:#2563eb;color:#fff;padding:12px 24px;
                          border-radius:6px;text-decoration:none;font-weight:bold;">
                  Verify Email
                </a>
                <br/><br/>
                <p>If you did not create an account, ignore this email.</p>
                <p>Or copy this link: {{VerificationUrl}}</p>
                """),

        // ── Forgot password ───────────────────────────────────────────────
        NotificationTemplate.Create(
            tenantId:        GlobalTenantId,
            eventType:       NotificationEventTypes.PasswordResetRequested,
            channel:         NotificationChannel.Email,
            subjectTemplate: "Reset your UMS password",
            bodyTemplate:    """
                <h2>Password Reset Request</h2>
                <p>We received a request to reset the password for <strong>{{Email}}</strong>.</p>
                <p>Click the button below to reset your password.
                   This link expires in <strong>{{ExpiresInHours}} hour</strong>.</p>
                <br/>
                <a href="{{ResetUrl}}"
                   style="background:#dc2626;color:#fff;padding:12px 24px;
                          border-radius:6px;text-decoration:none;font-weight:bold;">
                  Reset Password
                </a>
                <br/><br/>
                <p>If you did not request a password reset, ignore this email.
                   Your password will not change.</p>
                <p>Or copy this link: {{ResetUrl}}</p>
                """),

        // ── Password reset confirmation ───────────────────────────────────
        NotificationTemplate.Create(
            tenantId:        GlobalTenantId,
            eventType:       NotificationEventTypes.PasswordResetCompleted,
            channel:         NotificationChannel.Email,
            subjectTemplate: "Your UMS password was changed",
            bodyTemplate:    """
                <h2>Password Changed Successfully</h2>
                <p>Your password for <strong>{{Email}}</strong> was changed on
                   <strong>{{OccuredAt}}</strong>.</p>
                <p>If you made this change, no action is needed.</p>
                <p>If you did <strong>not</strong> make this change, please contact
                   your administrator immediately or reset your password.</p>
                <br/>
                <p>The UMS Team</p>
                """),

        // ── Account locked out ────────────────────────────────────────────
        NotificationTemplate.Create(
            tenantId:        GlobalTenantId,
            eventType:       NotificationEventTypes.UserLockedOut,
            channel:         NotificationChannel.Email,
            subjectTemplate: "Your UMS account has been locked",
            bodyTemplate:    """
                <h2>Account Temporarily Locked</h2>
                <p>Your account <strong>{{Email}}</strong> has been temporarily locked
                   due to too many failed login attempts.</p>
                <p>You can try again in <strong>15 minutes</strong>, or reset your
                   password if you have forgotten it.</p>
                <br/>
                <p>The UMS Team</p>
                """),
    ];
}
