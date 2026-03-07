// src/Services/Identity/Identity.Domain/Events/UserEmailVerifiedEvent.cs
using Identity.Domain.Common;

namespace Identity.Domain.Events;

public sealed class UserEmailVerifiedEvent : DomainEvent
{
    public Guid UserId { get; }
    public override Guid TenantId { get; }
    public string Email { get; }

    public UserEmailVerifiedEvent(
        Guid userId,
        Guid tenantId,
        string email)
    {
        UserId   = userId;
        TenantId = tenantId;
        Email    = email;
    }
}
