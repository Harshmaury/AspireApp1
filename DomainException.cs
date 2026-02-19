// src/Services/Identity/Identity.Domain/Exceptions/DomainException.cs
namespace Identity.Domain.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}