namespace Identity.Domain.Exceptions;

public sealed class UserAlreadyExistsException : DomainException
{
    public override string Code => "DUPLICATE_EMAIL";

    public UserAlreadyExistsException(string email)
        : base($"A user with email '{email}' already exists in this tenant.") { }
}
