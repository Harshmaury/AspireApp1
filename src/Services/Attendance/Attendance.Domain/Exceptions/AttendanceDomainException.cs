namespace Attendance.Domain.Exceptions;
public sealed class AttendanceDomainException : Exception
{
    public string Code { get; }
    public AttendanceDomainException(string code, string message) : base(message) => Code = code;
}
