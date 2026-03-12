// src/Services/Identity/Identity.Domain/Events/EmailVerificationRequestedEvent.cs
using UMS.SharedKernel.Domain;

namespace Identity.Domain.Events;

public sealed class EmailVerificationRequestedEvent : DomainEvent
{
    public Guid UserId { get; }
    public override Guid TenantId { get; }
    public string Email { get; }
    public string RawToken { get; }
    public DateTime ExpiresAt { get; }

    public EmailVerificationRequestedEvent(
        Guid userId,
        Guid tenantId,
        string email,
        string rawToken,
        DateTime expiresAt)
    {
        UserId    = userId;
        TenantId  = tenantId;
        Email     = email;
        RawToken  = rawToken;
        ExpiresAt = expiresAt;
    }
}
