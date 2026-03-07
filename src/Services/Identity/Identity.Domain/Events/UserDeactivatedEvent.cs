// src/Services/Identity/Identity.Domain/Events/UserDeactivatedEvent.cs
using Identity.Domain.Common;

namespace Identity.Domain.Events;

public sealed class UserDeactivatedEvent : DomainEvent
{
    public Guid UserId { get; }
    public override Guid TenantId { get; }
    public string Email { get; }

    public UserDeactivatedEvent(
        Guid userId,
        Guid tenantId,
        string email)
    {
        UserId   = userId;
        TenantId = tenantId;
        Email    = email;
    }
}
