namespace Identity.Domain.Exceptions;

public sealed class TenantAlreadyExistsException : DomainException
{
    public override string Code => "DUPLICATE_SLUG";

    public TenantAlreadyExistsException(string slug)
        : base($"A tenant with slug '{slug}' already exists.") { }
}
