using Academic.Domain.Common;
using Academic.Domain.Exceptions;
namespace Academic.Domain.Entities;
public sealed class Curriculum : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid ProgrammeId { get; private set; }
    public Guid CourseId { get; private set; }
    public int Semester { get; private set; }
    public bool IsElective { get; private set; }
    public bool IsOptional { get; private set; }
    public string Version { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    private Curriculum() { }

    public static Curriculum Create(Guid tenantId, Guid programmeId, Guid courseId, int semester, bool isElective, bool isOptional, string version)
    {
        if (semester < 1 || semester > 12) throw new AcademicDomainException("INVALID_SEMESTER", "Semester must be between 1 and 12.");
        if (string.IsNullOrWhiteSpace(version)) throw new AcademicDomainException("INVALID_VERSION", "Curriculum version is required.");
        return new Curriculum
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProgrammeId = programmeId,
            CourseId = courseId,
            Semester = semester,
            IsElective = isElective,
            IsOptional = isOptional,
            Version = version.Trim(),
            CreatedAt = DateTime.UtcNow
        };
    }
}