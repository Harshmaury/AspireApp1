namespace Examination.Domain.Exceptions;
public sealed class ExaminationDomainException : Exception, UMS.SharedKernel.Exceptions.IDomainException
{
    public string Code { get; }
    public ExaminationDomainException(string code, string message) : base(message) => Code = code;
}
