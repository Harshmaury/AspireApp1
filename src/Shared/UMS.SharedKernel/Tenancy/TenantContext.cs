namespace UMS.SharedKernel.Tenancy;

public sealed class TenantContext : ITenantContext
{
    private Guid   _tenantId;
    private string _slug = string.Empty;
    private string _tier = string.Empty;

    public Guid   TenantId   => IsResolved ? _tenantId
        : throw new InvalidOperationException("Tenant context not resolved.");
    public string Slug       => _slug;
    public string Tier       => _tier;
    public bool   IsResolved { get; private set; }

    public void SetTenant(Guid tenantId, string slug, string tier)
    {
        if (IsResolved)
            throw new InvalidOperationException("Tenant context already resolved.");
        _tenantId  = tenantId;
        _slug      = slug;
        _tier      = tier;
        IsResolved = true;
    }
}
