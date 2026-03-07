// src/Services/Identity/Identity.Domain/Exceptions/InvalidVerificationTokenException.cs
namespace Identity.Domain.Exceptions;

public sealed class InvalidVerificationTokenException : DomainException
{
    public override string Code => "INVALID_TOKEN";
    public InvalidVerificationTokenException()
        : base("The verification token is invalid.") { }
}
