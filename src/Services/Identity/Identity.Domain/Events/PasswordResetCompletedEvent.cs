// src/Services/Identity/Identity.Domain/Events/PasswordResetCompletedEvent.cs
using Identity.Domain.Common;

namespace Identity.Domain.Events;

public sealed class PasswordResetCompletedEvent : DomainEvent
{
    public Guid UserId { get; }
    public override Guid TenantId { get; }
    public string Email { get; }
    public string IpAddress { get; }

    public PasswordResetCompletedEvent(
        Guid userId,
        Guid tenantId,
        string email,
        string ipAddress)
    {
        UserId    = userId;
        TenantId  = tenantId;
        Email     = email;
        IpAddress = ipAddress;
    }
}
