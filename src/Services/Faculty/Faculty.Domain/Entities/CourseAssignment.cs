using Faculty.Domain.Common;
using Faculty.Domain.Enums;
using Faculty.Domain.Events;
using Faculty.Domain.Exceptions;
namespace Faculty.Domain.Entities;
public sealed class CourseAssignment : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid FacultyId { get; private set; }
    public Guid CourseId { get; private set; }
    public string AcademicYear { get; private set; } = default!;
    public int Semester { get; private set; }
    public string? Section { get; private set; }
    public DateTime AssignedAt { get; private set; }
    private CourseAssignment() { }
    public static CourseAssignment Create(Guid tenantId, Guid facultyId, Guid courseId, string academicYear, int semester, string? section = null)
    {
        if (string.IsNullOrWhiteSpace(academicYear)) throw new FacultyDomainException("INVALID_YEAR", "Academic year is required.");
        if (semester < 1 || semester > 12) throw new FacultyDomainException("INVALID_SEMESTER", "Invalid semester.");
        var assignment = new CourseAssignment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FacultyId = facultyId,
            CourseId = courseId,
            AcademicYear = academicYear.Trim(),
            Semester = semester,
            Section = section?.Trim().ToUpper(),
            AssignedAt = DateTime.UtcNow
        };
        assignment.RaiseDomainEvent(new CourseAssignedEvent(facultyId, tenantId, courseId, academicYear));
        return assignment;
    }
}
