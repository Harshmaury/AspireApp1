using Academic.Domain.Common;
using Academic.Domain.Enums;
using Academic.Domain.Events;
using Academic.Domain.Exceptions;
namespace Academic.Domain.Entities;
public sealed class Programme : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid DepartmentId { get; private set; }
    public string Name { get; private set; } = default!;
    public string Code { get; private set; } = default!;
    public string Degree { get; private set; } = default!;
    public int DurationYears { get; private set; }
    public int TotalCredits { get; private set; }
    public int IntakeCapacity { get; private set; }
    public ProgramStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Programme() { }

    public static Programme Create(Guid tenantId, Guid departmentId, string name, string code, string degree, int durationYears, int totalCredits, int intakeCapacity)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new AcademicDomainException("INVALID_NAME", "Programme name is required.");
        if (string.IsNullOrWhiteSpace(code)) throw new AcademicDomainException("INVALID_CODE", "Programme code is required.");
        if (durationYears < 1 || durationYears > 6) throw new AcademicDomainException("INVALID_DURATION", "Duration must be between 1 and 6 years.");
        if (totalCredits < 1) throw new AcademicDomainException("INVALID_CREDITS", "Total credits must be greater than 0.");
        if (intakeCapacity < 1) throw new AcademicDomainException("INVALID_INTAKE", "Intake capacity must be greater than 0.");
        return new Programme
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DepartmentId = departmentId,
            Name = name.Trim(),
            Code = code.Trim().ToUpperInvariant(),
            Degree = degree.Trim(),
            DurationYears = durationYears,
            TotalCredits = totalCredits,
            IntakeCapacity = intakeCapacity,
            Status = ProgramStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Activate()
    {
        if (Status == ProgramStatus.Retired) throw new AcademicDomainException("INVALID_STATE", "Cannot activate a retired programme.");
        Status = ProgramStatus.Active;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ProgrammeActivatedEvent(Id, TenantId, Code));
    }

    public void Retire()
    {
        if (Status == ProgramStatus.Draft) throw new AcademicDomainException("INVALID_STATE", "Cannot retire a draft programme.");
        Status = ProgramStatus.Retired;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(string name, int durationYears, int totalCredits, int intakeCapacity)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new AcademicDomainException("INVALID_NAME", "Programme name is required.");
        Name = name.Trim();
        DurationYears = durationYears;
        TotalCredits = totalCredits;
        IntakeCapacity = intakeCapacity;
        UpdatedAt = DateTime.UtcNow;
    }
}