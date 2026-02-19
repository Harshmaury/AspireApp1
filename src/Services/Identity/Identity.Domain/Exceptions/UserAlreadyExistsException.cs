namespace Identity.Domain.Exceptions;

public sealed class UserAlreadyExistsException : DomainException
{
    public UserAlreadyExistsException(string email)
        : base($"A user with email ''{email}'' already exists in this tenant.") { }
}
