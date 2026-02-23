namespace Fee.Domain.Exceptions;
public sealed class FeeDomainException : Exception, UMS.SharedKernel.Exceptions.IDomainException
{
    public string Code { get; }
    public FeeDomainException(string code, string message) : base(message) => Code = code;
}
