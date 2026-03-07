// src/Services/Identity/Identity.Domain/Events/TenantUpgradedEvent.cs
using Identity.Domain.Common;
using Identity.Domain.Entities;

namespace Identity.Domain.Events;

public sealed class TenantUpgradedEvent : DomainEvent
{
    public override Guid TenantId { get; }
    public string Slug { get; }
    public TenantTier OldTier { get; }
    public TenantTier NewTier { get; }

    public TenantUpgradedEvent(
        Guid tenantId,
        string slug,
        TenantTier oldTier,
        TenantTier newTier)
    {
        TenantId = tenantId;
        Slug     = slug;
        OldTier  = oldTier;
        NewTier  = newTier;
    }
}
