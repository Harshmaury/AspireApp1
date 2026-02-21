using Attendance.Domain.Common;
using Attendance.Domain.Events;
using Attendance.Domain.Exceptions;
namespace Attendance.Domain.Entities;
public sealed class AttendanceSummary : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid StudentId { get; private set; }
    public Guid CourseId { get; private set; }
    public string AcademicYear { get; private set; } = default!;
    public int Semester { get; private set; }
    public int TotalClasses { get; private set; }
    public int AttendedClasses { get; private set; }
    public decimal Percentage { get; private set; }
    public bool IsShortage { get; private set; }
    public bool IsWarning { get; private set; }
    public DateTime LastUpdated { get; private set; }
    private AttendanceSummary() { }
    public static AttendanceSummary Create(Guid tenantId, Guid studentId, Guid courseId, string academicYear, int semester)
    {
        if (string.IsNullOrWhiteSpace(academicYear)) throw new AttendanceDomainException("INVALID_YEAR", "Academic year is required.");
        if (semester < 1 || semester > 12) throw new AttendanceDomainException("INVALID_SEMESTER", "Invalid semester.");
        return new AttendanceSummary
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StudentId = studentId,
            CourseId = courseId,
            AcademicYear = academicYear.Trim(),
            Semester = semester,
            TotalClasses = 0,
            AttendedClasses = 0,
            Percentage = 100,
            IsShortage = false,
            IsWarning = false,
            LastUpdated = DateTime.UtcNow
        };
    }
    public void Refresh(int totalClasses, int attendedClasses)
    {
        if (totalClasses < 0) throw new AttendanceDomainException("INVALID_TOTAL", "Total classes cannot be negative.");
        if (attendedClasses < 0 || attendedClasses > totalClasses) throw new AttendanceDomainException("INVALID_ATTENDED", "Attended classes must be between 0 and total classes.");
        TotalClasses = totalClasses;
        AttendedClasses = attendedClasses;
        Percentage = totalClasses == 0 ? 100m : Math.Round((decimal)attendedClasses / totalClasses * 100, 2);
        IsWarning = Percentage < 80m && Percentage >= 75m;
        IsShortage = Percentage < 75m;
        LastUpdated = DateTime.UtcNow;
        if (IsShortage)
            RaiseDomainEvent(new AttendanceShortageFlaggedEvent(StudentId, TenantId, CourseId, Percentage));
    }
    public bool IsEligibleForExam() => Percentage >= 75m;
}
