namespace Microsoft.Extensions.DependencyInjection;

public interface ITenantContext
{
    Guid TenantId { get; }
    string TenantSlug { get; }
    bool IsResolved { get; }
}

public sealed class TenantContext : ITenantContext
{
    public Guid TenantId { get; init; }
    public string TenantSlug { get; init; } = string.Empty;
    public bool IsResolved { get; init; }
}
