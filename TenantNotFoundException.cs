// src/Services/Identity/Identity.Domain/Exceptions/TenantNotFoundException.cs
namespace Identity.Domain.Exceptions;

public sealed class TenantNotFoundException : DomainException
{
    public TenantNotFoundException(Guid tenantId)
        : base($"Tenant with ID '{tenantId}' was not found.") { }
}