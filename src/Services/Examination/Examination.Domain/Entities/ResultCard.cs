using Examination.Domain.Common;
using Examination.Domain.Events;
using Examination.Domain.Exceptions;
namespace Examination.Domain.Entities;
public sealed class ResultCard : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid StudentId { get; private set; }
    public string AcademicYear { get; private set; } = default!;
    public int Semester { get; private set; }
    public decimal SGPA { get; private set; }
    public decimal CGPA { get; private set; }
    public int TotalCreditsEarned { get; private set; }
    public int TotalCreditsAttempted { get; private set; }
    public bool HasBacklog { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    private ResultCard() { }
    public static ResultCard Create(Guid tenantId, Guid studentId, string academicYear, int semester, decimal sgpa, decimal cgpa, int creditsEarned, int creditsAttempted)
    {
        if (string.IsNullOrWhiteSpace(academicYear)) throw new ExaminationDomainException("INVALID_YEAR", "Academic year is required.");
        if (semester < 1 || semester > 12) throw new ExaminationDomainException("INVALID_SEMESTER", "Invalid semester.");
        if (sgpa < 0 || sgpa > 10) throw new ExaminationDomainException("INVALID_SGPA", "SGPA must be between 0 and 10.");
        if (cgpa < 0 || cgpa > 10) throw new ExaminationDomainException("INVALID_CGPA", "CGPA must be between 0 and 10.");
        if (creditsEarned > creditsAttempted) throw new ExaminationDomainException("INVALID_CREDITS", "Credits earned cannot exceed credits attempted.");
        var result = new ResultCard
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StudentId = studentId,
            AcademicYear = academicYear.Trim(),
            Semester = semester,
            SGPA = sgpa,
            CGPA = cgpa,
            TotalCreditsEarned = creditsEarned,
            TotalCreditsAttempted = creditsAttempted,
            HasBacklog = creditsEarned < creditsAttempted,
            CreatedAt = DateTime.UtcNow
        };
        if (result.HasBacklog)
            result.RaiseDomainEvent(new StudentBacklogEvent(studentId, tenantId, Guid.Empty));
        return result;
    }
    public void Publish()
    {
        if (PublishedAt.HasValue) throw new ExaminationDomainException("ALREADY_PUBLISHED", "Result card is already published.");
        PublishedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ResultDeclaredEvent(StudentId, TenantId, AcademicYear, Semester, SGPA, CGPA));
    }
}
