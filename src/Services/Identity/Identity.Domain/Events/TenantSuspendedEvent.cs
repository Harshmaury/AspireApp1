// src/Services/Identity/Identity.Domain/Events/TenantSuspendedEvent.cs
using Identity.Domain.Common;

namespace Identity.Domain.Events;

public sealed class TenantSuspendedEvent : DomainEvent
{
    public override Guid TenantId { get; }
    public string Slug { get; }

    public TenantSuspendedEvent(
        Guid tenantId,
        string slug)
    {
        TenantId = tenantId;
        Slug     = slug;
    }
}
