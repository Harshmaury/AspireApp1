// src/Services/Identity/Identity.Domain/Events/TenantProvisionedEvent.cs
using UMS.SharedKernel.Domain;
using Identity.Domain.Entities;

namespace Identity.Domain.Events;

public sealed class TenantProvisionedEvent : DomainEvent
{
    public override Guid TenantId { get; }
    public string Name { get; }
    public string Slug { get; }
    public TenantTier Tier { get; }
    public string Region { get; }

    public TenantProvisionedEvent(
        Guid tenantId,
        string name,
        string slug,
        TenantTier tier,
        string region)
    {
        TenantId = tenantId;
        Name     = name;
        Slug     = slug;
        Tier     = tier;
        Region   = region;
    }
}
