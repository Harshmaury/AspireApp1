namespace Identity.Domain.Exceptions;

public sealed class TenantNotFoundException : DomainException
{
    public override string Code => "TENANT_NOT_FOUND";

    public TenantNotFoundException(Guid tenantId)
        : base($"Tenant '{tenantId}' was not found.") { }
}
