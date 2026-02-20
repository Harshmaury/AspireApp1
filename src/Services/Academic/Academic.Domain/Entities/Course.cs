using Academic.Domain.Common;
using Academic.Domain.Enums;
using Academic.Domain.Events;
using Academic.Domain.Exceptions;
namespace Academic.Domain.Entities;
public sealed class Course : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid DepartmentId { get; private set; }
    public string Name { get; private set; } = default!;
    public string Code { get; private set; } = default!;
    public string? Description { get; private set; }
    public int Credits { get; private set; }
    public int LectureHours { get; private set; }
    public int TutorialHours { get; private set; }
    public int PracticalHours { get; private set; }
    public string CourseType { get; private set; } = default!;
    public int MaxEnrollment { get; private set; }
    public CourseStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Course() { }

    public static Course Create(Guid tenantId, Guid departmentId, string name, string code, int credits, string courseType, int lectureHours, int tutorialHours, int practicalHours, int maxEnrollment, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new AcademicDomainException("INVALID_NAME", "Course name is required.");
        if (string.IsNullOrWhiteSpace(code)) throw new AcademicDomainException("INVALID_CODE", "Course code is required.");
        if (credits < 1 || credits > 6) throw new AcademicDomainException("INVALID_CREDITS", "Credits must be between 1 and 6.");
        return new Course
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DepartmentId = departmentId,
            Name = name.Trim(),
            Code = code.Trim().ToUpperInvariant(),
            Description = description?.Trim(),
            Credits = credits,
            LectureHours = lectureHours,
            TutorialHours = tutorialHours,
            PracticalHours = practicalHours,
            CourseType = courseType.Trim(),
            MaxEnrollment = maxEnrollment,
            Status = CourseStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Publish()
    {
        if (Status == CourseStatus.Retired) throw new AcademicDomainException("INVALID_STATE", "Cannot publish a retired course.");
        Status = CourseStatus.Published;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new CoursePublishedEvent(Id, TenantId, Code, Name));
    }

    public void Retire()
    {
        if (Status == CourseStatus.Draft) throw new AcademicDomainException("INVALID_STATE", "Cannot retire a draft course.");
        Status = CourseStatus.Retired;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(string name, string? description, int credits, string courseType, int maxEnrollment)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new AcademicDomainException("INVALID_NAME", "Course name is required.");
        Name = name.Trim();
        Description = description?.Trim();
        Credits = credits;
        CourseType = courseType.Trim();
        MaxEnrollment = maxEnrollment;
        UpdatedAt = DateTime.UtcNow;
    }
}