// src/Services/Identity/Identity.Domain/Events/UserLoginFailedEvent.cs
using Identity.Domain.Common;

namespace Identity.Domain.Events;

public sealed class UserLoginFailedEvent : DomainEvent
{
    public override Guid TenantId { get; }
    public string Email { get; }
    public string Reason { get; }
    public string IpAddress { get; }

    public UserLoginFailedEvent(
        Guid tenantId,
        string email,
        string reason,
        string ipAddress)
    {
        TenantId  = tenantId;
        Email     = email;
        Reason    = reason;
        IpAddress = ipAddress;
    }
}
