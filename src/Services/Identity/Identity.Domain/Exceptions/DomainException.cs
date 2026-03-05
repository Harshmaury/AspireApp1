using UMS.SharedKernel.Exceptions;

namespace Identity.Domain.Exceptions;

public abstract class DomainException : Exception, IDomainException
{
    public abstract string Code { get; }
    protected DomainException(string message) : base(message) { }
}
