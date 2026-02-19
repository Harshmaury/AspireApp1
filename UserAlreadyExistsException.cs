// src/Services/Identity/Identity.Domain/Exceptions/UserAlreadyExistsException.cs
namespace Identity.Domain.Exceptions;

public sealed class UserAlreadyExistsException : DomainException
{
    public UserAlreadyExistsException(string email)
        : base($"A user with email '{email}' already exists in this tenant.") { }
}