using Identity.Domain.Common;

namespace Identity.Domain.Events;

public sealed class UserRegisteredEvent : DomainEvent
{
    public Guid UserId { get; }
    public Guid TenantId { get; }
    public string Email { get; }

    public UserRegisteredEvent(Guid userId, Guid tenantId, string email)
    {
        UserId = userId;
        TenantId = tenantId;
        Email = email;
    }
}
