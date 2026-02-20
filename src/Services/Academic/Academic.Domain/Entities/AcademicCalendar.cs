using Academic.Domain.Common;
using Academic.Domain.Exceptions;
namespace Academic.Domain.Entities;
public sealed class AcademicCalendar : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string AcademicYear { get; private set; } = default!;
    public int Semester { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public DateTime ExamStartDate { get; private set; }
    public DateTime ExamEndDate { get; private set; }
    public DateTime RegistrationOpenDate { get; private set; }
    public DateTime RegistrationCloseDate { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private AcademicCalendar() { }

    public static AcademicCalendar Create(Guid tenantId, string academicYear, int semester, DateTime startDate, DateTime endDate, DateTime examStartDate, DateTime examEndDate, DateTime registrationOpenDate, DateTime registrationCloseDate)
    {
        if (string.IsNullOrWhiteSpace(academicYear)) throw new AcademicDomainException("INVALID_YEAR", "Academic year is required.");
        if (semester < 1 || semester > 2) throw new AcademicDomainException("INVALID_SEMESTER", "Semester must be 1 or 2.");
        if (endDate <= startDate) throw new AcademicDomainException("INVALID_DATES", "End date must be after start date.");
        if (examEndDate <= examStartDate) throw new AcademicDomainException("INVALID_EXAM_DATES", "Exam end date must be after exam start date.");
        return new AcademicCalendar
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AcademicYear = academicYear.Trim(),
            Semester = semester,
            StartDate = startDate,
            EndDate = endDate,
            ExamStartDate = examStartDate,
            ExamEndDate = examEndDate,
            RegistrationOpenDate = registrationOpenDate,
            RegistrationCloseDate = registrationCloseDate,
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Activate() { IsActive = true; UpdatedAt = DateTime.UtcNow; }
    public void Deactivate() { IsActive = false; UpdatedAt = DateTime.UtcNow; }
}