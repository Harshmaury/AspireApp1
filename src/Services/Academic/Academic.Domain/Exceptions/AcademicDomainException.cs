namespace Academic.Domain.Exceptions;
public sealed class AcademicDomainException : Exception, UMS.SharedKernel.Exceptions.IDomainException
{
    public string Code { get; }
    public AcademicDomainException(string code, string message) : base(message) => Code = code;
}
