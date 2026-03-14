// UMS — University Management System
// Key:     UMS-NOTIFICATION-P2-003
// Service: Notification
// Layer:   Application / Events
// These are the contracts the Notification service uses to deserialize
// events published by the Identity service via Kafka.
namespace Notification.Application.Events;

// ── Already exists — keeping for reference ────────────────────────────────
// public sealed record UserRegisteredEvent(...)

// ── Updated with ResetUrl / VerificationUrl fields ────────────────────────

public sealed record EmailVerificationRequestedEvent(
    Guid   TenantId,
    Guid   UserId,
    string Email,
    string VerificationUrl,   // full URL with token embedded
    string Action,
    string Details);

public sealed record PasswordResetRequestedEvent(
    Guid   TenantId,
    Guid   UserId,
    string Email,
    string ResetUrl,          // full URL with token embedded
    string Action,
    string Details);

public sealed record PasswordResetCompletedEvent(
    Guid            TenantId,
    Guid            UserId,
    string          Email,
    DateTimeOffset  OccurredAt,
    string          Action,
    string          Details);
