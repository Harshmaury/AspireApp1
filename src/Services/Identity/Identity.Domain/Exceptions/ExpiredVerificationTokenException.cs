// src/Services/Identity/Identity.Domain/Exceptions/ExpiredVerificationTokenException.cs
namespace Identity.Domain.Exceptions;

public sealed class ExpiredVerificationTokenException : DomainException
{
    public override string Code => "TOKEN_EXPIRED";
    public ExpiredVerificationTokenException()
        : base("The verification token has expired. Please request a new one.") { }
}
