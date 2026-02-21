using Examination.Domain.Common;
using Examination.Domain.Enums;
using Examination.Domain.Events;
using Examination.Domain.Exceptions;
namespace Examination.Domain.Entities;
public sealed class MarksEntry : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid StudentId { get; private set; }
    public Guid ExamScheduleId { get; private set; }
    public Guid CourseId { get; private set; }
    public decimal MarksObtained { get; private set; }
    public string Grade { get; private set; } = default!;
    public decimal GradePoint { get; private set; }
    public bool IsAbsent { get; private set; }
    public Guid EnteredBy { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public MarksStatus Status { get; private set; }
    public DateTime EnteredAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    private MarksEntry() { }
    private static (string Grade, decimal GradePoint) ComputeGrade(decimal marks, int maxMarks)
    {
        var percentage = marks / maxMarks * 100;
        return percentage switch
        {
            >= 90 => ("O", 10),
            >= 80 => ("A+", 9),
            >= 70 => ("A", 8),
            >= 60 => ("B+", 7),
            >= 50 => ("B", 6),
            >= 45 => ("C", 5),
            >= 40 => ("P", 4),
            _ => ("F", 0)
        };
    }
    public static MarksEntry Create(Guid tenantId, Guid studentId, Guid examScheduleId, Guid courseId, decimal marksObtained, int maxMarks, bool isAbsent, Guid enteredBy)
    {
        if (marksObtained < 0 || marksObtained > maxMarks) throw new ExaminationDomainException("INVALID_MARKS", "Marks must be between 0 and max marks.");
        var (grade, gradePoint) = isAbsent ? ("F", 0m) : ComputeGrade(marksObtained, maxMarks);
        return new MarksEntry
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StudentId = studentId,
            ExamScheduleId = examScheduleId,
            CourseId = courseId,
            MarksObtained = isAbsent ? 0 : marksObtained,
            Grade = grade,
            GradePoint = gradePoint,
            IsAbsent = isAbsent,
            EnteredBy = enteredBy,
            Status = MarksStatus.Draft,
            EnteredAt = DateTime.UtcNow
        };
    }
    public void Submit()
    {
        if (Status != MarksStatus.Draft) throw new ExaminationDomainException("INVALID_STATUS", "Only draft marks can be submitted.");
        Status = MarksStatus.Submitted; UpdatedAt = DateTime.UtcNow;
    }
    public void Approve(Guid approvedBy)
    {
        if (Status != MarksStatus.Submitted) throw new ExaminationDomainException("INVALID_STATUS", "Only submitted marks can be approved.");
        ApprovedBy = approvedBy; Status = MarksStatus.Approved; UpdatedAt = DateTime.UtcNow;
    }
    public void Publish()
    {
        if (Status != MarksStatus.Approved) throw new ExaminationDomainException("INVALID_STATUS", "Only approved marks can be published.");
        Status = MarksStatus.Published; UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new MarksPublishedEvent(ExamScheduleId, TenantId));
    }
}
