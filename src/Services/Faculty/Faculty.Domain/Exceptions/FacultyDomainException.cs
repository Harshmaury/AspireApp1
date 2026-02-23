namespace Faculty.Domain.Exceptions;
public sealed class FacultyDomainException : Exception, UMS.SharedKernel.Exceptions.IDomainException
{
    public string Code { get; }
    public FacultyDomainException(string code, string message) : base(message) => Code = code;
}
