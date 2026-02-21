namespace Hostel.Domain.Exceptions;
public sealed class HostelDomainException : Exception
{
    public string Code { get; }
    public HostelDomainException(string code, string message) : base(message) => Code = code;
}
