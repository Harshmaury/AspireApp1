using Identity.Domain.Common;
using Identity.Domain.Events;

namespace Identity.Domain.Entities;

public enum TenantTier { Shared, Dedicated, Enterprise }
public enum SubscriptionStatus { Trial, Active, Suspended, Cancelled }

public sealed class Tenant : IAggregateRoot
{
    // ── IAggregateRoot ────────────────────────────────────────────────────────
    // Tenant cannot inherit BaseEntity (no base class available), so the
    // events list is implemented inline — identical pattern to ApplicationUser.
    private readonly List<DomainEvent> _domainEvents = [];
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();
    private void RaiseDomainEvent(DomainEvent e) => _domainEvents.Add(e);

    // ── Properties ────────────────────────────────────────────────────────────
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? LogoUrl { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public TenantTier Tier { get; private set; } = TenantTier.Shared;
    public SubscriptionStatus SubscriptionStatus { get; private set; } = SubscriptionStatus.Trial;
    public int MaxUsers { get; private set; } = 100;
    public string Region { get; private set; } = "default";

    // Legacy — retained for DB expand/contract. Do not use in new code.
    public string FeaturesJson { get; private set; } = "{}";

    // Structured feature flags — use this going forward
    public TenantFeatures Features { get; private set; } = TenantFeatures.Default();

    // Optimistic concurrency token — managed by EF, never set manually
    public byte[]? RowVersion { get; private set; }

    private Tenant() { }

    // ── Factory ───────────────────────────────────────────────────────────────
    public static Tenant Create(string name, string slug,
        TenantTier tier = TenantTier.Shared, string region = "default")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        var tenant = new Tenant
        {
            Id                 = Guid.NewGuid(),
            Name               = name,
            Slug               = slug.ToLowerInvariant(),
            IsActive           = true,
            CreatedAt          = DateTime.UtcNow,
            UpdatedAt          = DateTime.UtcNow,
            Tier               = tier,
            Region             = region,
            SubscriptionStatus = SubscriptionStatus.Trial,
            MaxUsers           = tier == TenantTier.Enterprise ? 10000 :
                                 tier == TenantTier.Dedicated  ? 1000  : 100,
            Features           = TenantFeatures.Default()
        };

        tenant.RaiseDomainEvent(new TenantProvisionedEvent(
            tenant.Id, tenant.Name, tenant.Slug, tenant.Tier, tenant.Region));

        return tenant;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public void Activate()
    {
        if (SubscriptionStatus == SubscriptionStatus.Cancelled)
            throw new InvalidOperationException(
                "A cancelled tenant cannot be activated. Provision a new tenant instead.");

        IsActive           = true;
        SubscriptionStatus = SubscriptionStatus.Active;
        UpdatedAt          = DateTime.UtcNow;
    }

    public void Suspend()
    {
        if (SubscriptionStatus == SubscriptionStatus.Cancelled)
            throw new InvalidOperationException("A cancelled tenant cannot be suspended.");

        if (SubscriptionStatus == SubscriptionStatus.Suspended)
            return; // idempotent

        IsActive           = false;
        SubscriptionStatus = SubscriptionStatus.Suspended;
        UpdatedAt          = DateTime.UtcNow;

        RaiseDomainEvent(new TenantSuspendedEvent(Id, Slug));
    }

    public void Reinstate()
    {
        if (SubscriptionStatus != SubscriptionStatus.Suspended)
            throw new InvalidOperationException(
                $"Only a suspended tenant can be reinstated. Current status: {SubscriptionStatus}.");

        IsActive           = true;
        SubscriptionStatus = SubscriptionStatus.Active;
        UpdatedAt          = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (SubscriptionStatus == SubscriptionStatus.Cancelled)
            return; // idempotent

        IsActive           = false;
        SubscriptionStatus = SubscriptionStatus.Cancelled;
        UpdatedAt          = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (SubscriptionStatus == SubscriptionStatus.Cancelled)
            throw new InvalidOperationException("A cancelled tenant cannot be deactivated.");

        IsActive  = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Upgrade(TenantTier newTier)
    {
        if (SubscriptionStatus == SubscriptionStatus.Cancelled)
            throw new InvalidOperationException("A cancelled tenant cannot be upgraded.");

        var oldTier = Tier;
        Tier        = newTier;
        MaxUsers    = newTier == TenantTier.Enterprise ? 10000 :
                      newTier == TenantTier.Dedicated  ? 1000  : 100;
        UpdatedAt   = DateTime.UtcNow;

        RaiseDomainEvent(new TenantUpgradedEvent(Id, Slug, oldTier, newTier));
    }

    public void UpdateFeatures(TenantFeatures features)
    {
        ArgumentNullException.ThrowIfNull(features);
        Features  = features;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool CanAddUsers(int currentCount) => currentCount < MaxUsers;

    /// <summary>
    /// Checks a feature flag by name. Delegates to structured Features model.
    /// FeaturesJson is no longer consulted.
    /// </summary>
    public bool HasFeature(string feature) => Features.IsEnabled(feature);
}
