// src/Services/Identity/Identity.Domain/Exceptions/SelfRegistrationDisabledException.cs
namespace Identity.Domain.Exceptions;

public sealed class SelfRegistrationDisabledException : DomainException
{
    public override string Code => "SELF_REGISTRATION_DISABLED";
    public SelfRegistrationDisabledException(string tenantSlug)
        : base($"Self-registration is not allowed for tenant '{tenantSlug}'.") { }
}
