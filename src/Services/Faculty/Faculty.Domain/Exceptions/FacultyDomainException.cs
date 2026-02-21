namespace Faculty.Domain.Exceptions;
public sealed class FacultyDomainException : Exception
{
    public string Code { get; }
    public FacultyDomainException(string code, string message) : base(message) => Code = code;
}
