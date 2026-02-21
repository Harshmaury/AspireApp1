using Examination.Domain.Common;
using Examination.Domain.Enums;
using Examination.Domain.Events;
using Examination.Domain.Exceptions;
namespace Examination.Domain.Entities;
public sealed class ExamSchedule : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid CourseId { get; private set; }
    public string AcademicYear { get; private set; } = default!;
    public int Semester { get; private set; }
    public ExamType ExamType { get; private set; }
    public DateTime ExamDate { get; private set; }
    public int Duration { get; private set; }
    public string Venue { get; private set; } = default!;
    public int MaxMarks { get; private set; }
    public int PassingMarks { get; private set; }
    public ExamStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    private ExamSchedule() { }
    public static ExamSchedule Create(Guid tenantId, Guid courseId, string academicYear, int semester, ExamType examType, DateTime examDate, int duration, string venue, int maxMarks, int passingMarks)
    {
        if (string.IsNullOrWhiteSpace(academicYear)) throw new ExaminationDomainException("INVALID_YEAR", "Academic year is required.");
        if (semester < 1 || semester > 12) throw new ExaminationDomainException("INVALID_SEMESTER", "Invalid semester.");
        if (duration <= 0) throw new ExaminationDomainException("INVALID_DURATION", "Duration must be positive.");
        if (string.IsNullOrWhiteSpace(venue)) throw new ExaminationDomainException("INVALID_VENUE", "Venue is required.");
        if (maxMarks <= 0) throw new ExaminationDomainException("INVALID_MARKS", "Max marks must be positive.");
        if (passingMarks <= 0 || passingMarks > maxMarks) throw new ExaminationDomainException("INVALID_PASSING_MARKS", "Passing marks must be between 1 and max marks.");
        var schedule = new ExamSchedule
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CourseId = courseId,
            AcademicYear = academicYear.Trim(),
            Semester = semester,
            ExamType = examType,
            ExamDate = examDate,
            Duration = duration,
            Venue = venue.Trim(),
            MaxMarks = maxMarks,
            PassingMarks = passingMarks,
            Status = ExamStatus.Scheduled,
            CreatedAt = DateTime.UtcNow
        };
        schedule.RaiseDomainEvent(new ExamScheduledEvent(schedule.Id, schedule.TenantId, schedule.CourseId));
        return schedule;
    }
    public void Start()
    {
        if (Status != ExamStatus.Scheduled) throw new ExaminationDomainException("INVALID_STATUS", "Only scheduled exams can be started.");
        Status = ExamStatus.Ongoing; UpdatedAt = DateTime.UtcNow;
    }
    public void Complete()
    {
        if (Status != ExamStatus.Ongoing) throw new ExaminationDomainException("INVALID_STATUS", "Only ongoing exams can be completed.");
        Status = ExamStatus.Completed; UpdatedAt = DateTime.UtcNow;
    }
    public void Cancel()
    {
        if (Status == ExamStatus.Completed) throw new ExaminationDomainException("INVALID_STATUS", "Completed exams cannot be cancelled.");
        Status = ExamStatus.Cancelled; UpdatedAt = DateTime.UtcNow;
    }
}
