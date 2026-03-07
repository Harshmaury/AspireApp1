// src/Services/Identity/Identity.Domain/Exceptions/TenantUserLimitExceededException.cs
namespace Identity.Domain.Exceptions;

public sealed class TenantUserLimitExceededException : DomainException
{
    public override string Code => "USER_LIMIT_EXCEEDED";
    public TenantUserLimitExceededException(Guid tenantId, int maxUsers)
        : base($"Tenant '{tenantId}' has reached its user limit of {maxUsers}.") { }
}
