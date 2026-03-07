// src/Services/Identity/Identity.Domain/Events/PasswordResetRequestedEvent.cs
using Identity.Domain.Common;

namespace Identity.Domain.Events;

public sealed class PasswordResetRequestedEvent : DomainEvent
{
    public Guid UserId { get; }
    public override Guid TenantId { get; }
    public string Email { get; }
    public string RawToken { get; }
    public DateTime ExpiresAt { get; }
    public string IpAddress { get; }

    public PasswordResetRequestedEvent(
        Guid userId,
        Guid tenantId,
        string email,
        string rawToken,
        DateTime expiresAt,
        string ipAddress)
    {
        UserId    = userId;
        TenantId  = tenantId;
        Email     = email;
        RawToken  = rawToken;
        ExpiresAt = expiresAt;
        IpAddress = ipAddress;
    }
}
