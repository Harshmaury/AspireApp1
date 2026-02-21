namespace Fee.Domain.Exceptions;
public sealed class FeeDomainException : Exception
{
    public string Code { get; }
    public FeeDomainException(string code, string message) : base(message) => Code = code;
}
