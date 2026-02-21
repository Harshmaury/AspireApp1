using Attendance.Domain.Common;
using Attendance.Domain.Enums;
using Attendance.Domain.Events;
using Attendance.Domain.Exceptions;
namespace Attendance.Domain.Entities;
public sealed class AttendanceRecord : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid StudentId { get; private set; }
    public Guid CourseId { get; private set; }
    public string AcademicYear { get; private set; } = default!;
    public int Semester { get; private set; }
    public DateOnly Date { get; private set; }
    public ClassType ClassType { get; private set; }
    public bool IsPresent { get; private set; }
    public Guid MarkedBy { get; private set; }
    public DateTime MarkedAt { get; private set; }
    public bool IsLocked { get; private set; }
    private AttendanceRecord() { }
    public static AttendanceRecord Create(Guid tenantId, Guid studentId, Guid courseId, string academicYear, int semester, DateOnly date, ClassType classType, bool isPresent, Guid markedBy)
    {
        if (string.IsNullOrWhiteSpace(academicYear)) throw new AttendanceDomainException("INVALID_YEAR", "Academic year is required.");
        if (semester < 1 || semester > 12) throw new AttendanceDomainException("INVALID_SEMESTER", "Invalid semester.");
        if (date > DateOnly.FromDateTime(DateTime.UtcNow)) throw new AttendanceDomainException("INVALID_DATE", "Cannot mark attendance for a future date.");
        if (date < DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7))) throw new AttendanceDomainException("BACKDATING_NOT_ALLOWED", "Attendance cannot be marked more than 7 days in the past.");
        var record = new AttendanceRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StudentId = studentId,
            CourseId = courseId,
            AcademicYear = academicYear.Trim(),
            Semester = semester,
            Date = date,
            ClassType = classType,
            IsPresent = isPresent,
            MarkedBy = markedBy,
            MarkedAt = DateTime.UtcNow,
            IsLocked = false
        };
        record.RaiseDomainEvent(new AttendanceMarkedEvent(studentId, tenantId, courseId, date, isPresent));
        return record;
    }
    public void Lock()
    {
        if (IsLocked) throw new AttendanceDomainException("ALREADY_LOCKED", "Attendance record is already locked.");
        IsLocked = true;
    }
    public void Correct(bool isPresent, Guid correctedBy)
    {
        if (IsLocked) throw new AttendanceDomainException("RECORD_LOCKED", "Cannot correct a locked attendance record.");
        IsPresent = isPresent;
        MarkedBy = correctedBy;
        MarkedAt = DateTime.UtcNow;
    }
}
