namespace Attendance.Domain.Exceptions;
public sealed class AttendanceDomainException : Exception, UMS.SharedKernel.Exceptions.IDomainException
{
    public string Code { get; }
    public AttendanceDomainException(string code, string message) : base(message) => Code = code;
}
