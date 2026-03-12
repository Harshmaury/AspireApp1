// src/Services/Identity/Identity.Domain/Events/UserLockedOutEvent.cs
using UMS.SharedKernel.Domain;

namespace Identity.Domain.Events;

public sealed class UserLockedOutEvent : DomainEvent
{
    public Guid UserId { get; }
    public override Guid TenantId { get; }
    public string Email { get; }
    public string IpAddress { get; }

    public UserLockedOutEvent(
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
