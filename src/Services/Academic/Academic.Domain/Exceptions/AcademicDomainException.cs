namespace Academic.Domain.Exceptions;
public sealed class AcademicDomainException : Exception
{
    public string Code { get; }
    public AcademicDomainException(string code, string message) : base(message) => Code = code;
}