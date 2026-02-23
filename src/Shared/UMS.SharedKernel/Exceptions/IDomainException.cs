namespace UMS.SharedKernel.Exceptions;

public interface IDomainException
{
    string Code { get; }
    string Message { get; }
}
