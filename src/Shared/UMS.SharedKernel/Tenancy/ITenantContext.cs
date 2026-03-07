namespace UMS.SharedKernel.Tenancy;

public interface ITenantContext
{
    Guid TenantId { get; }
    string Slug { get; }
    string Tier { get; }
    bool IsResolved { get; }
}