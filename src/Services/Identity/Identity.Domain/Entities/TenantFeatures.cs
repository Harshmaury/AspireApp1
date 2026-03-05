namespace Identity.Domain.Entities;

/// <summary>
/// Strongly-typed feature flags for a tenant.
/// Replaces the stringly-typed FeaturesJson field.
/// </summary>
public sealed class TenantFeatures
{
    /// <summary>Users can self-register without admin invitation.</summary>
    public bool AllowSelfRegistration { get; private set; }

    /// <summary>Guest (unauthenticated) read access to public resources.</summary>
    public bool AllowGuestAccess { get; private set; }

    /// <summary>Multi-factor authentication enforcement.</summary>
    public bool EnableMfa { get; private set; }

    /// <summary>Full audit log of all tenant mutations.</summary>
    public bool EnableAuditLog { get; private set; }

    private TenantFeatures() { }

    public static TenantFeatures Default() => new();

    public TenantFeatures WithSelfRegistration(bool value)
    {
        AllowSelfRegistration = value;
        return this;
    }

    public TenantFeatures WithGuestAccess(bool value)
    {
        AllowGuestAccess = value;
        return this;
    }

    public TenantFeatures WithMfa(bool value)
    {
        EnableMfa = value;
        return this;
    }

    public TenantFeatures WithAuditLog(bool value)
    {
        EnableAuditLog = value;
        return this;
    }

    public bool IsEnabled(string featureName) => featureName switch
    {
        nameof(AllowSelfRegistration) => AllowSelfRegistration,
        nameof(AllowGuestAccess)      => AllowGuestAccess,
        nameof(EnableMfa)             => EnableMfa,
        nameof(EnableAuditLog)        => EnableAuditLog,
        _                             => false
    };
}