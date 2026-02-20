namespace Identity.Domain.Entities;

public enum TenantTier { Shared, Dedicated, Enterprise }
public enum SubscriptionStatus { Trial, Active, Suspended, Cancelled }

public sealed class Tenant
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? LogoUrl { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public TenantTier Tier { get; private set; } = TenantTier.Shared;
    public SubscriptionStatus SubscriptionStatus { get; private set; } = SubscriptionStatus.Trial;
    public int MaxUsers { get; private set; } = 100;
    public string Region { get; private set; } = "default";
    public string FeaturesJson { get; private set; } = "{}";

    private Tenant() { }

    public static Tenant Create(string name, string slug,
        TenantTier tier = TenantTier.Shared, string region = "default")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        return new Tenant
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug.ToLowerInvariant(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Tier = tier,
            Region = region,
            SubscriptionStatus = SubscriptionStatus.Trial,
            MaxUsers = tier == TenantTier.Enterprise ? 10000 :
                       tier == TenantTier.Dedicated ? 1000 : 100
        };
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
    public void Suspend() => SubscriptionStatus = SubscriptionStatus.Suspended;
    public void Upgrade(TenantTier tier)
    {
        Tier = tier;
        MaxUsers = tier == TenantTier.Enterprise ? 10000 :
                   tier == TenantTier.Dedicated ? 1000 : 100;
    }
    public bool CanAddUsers(int currentCount) => currentCount < MaxUsers;
    public bool HasFeature(string feature) =>
        FeaturesJson.Contains(string.Concat("\"", feature, "\":true"));
}

