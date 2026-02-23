namespace Hostel.Domain.Exceptions;
public sealed class HostelDomainException : Exception, UMS.SharedKernel.Exceptions.IDomainException
{
    public string Code { get; }
    public HostelDomainException(string code, string message) : base(message) => Code = code;
}
