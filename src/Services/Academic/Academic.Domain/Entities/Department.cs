using Academic.Domain.Common;
using Academic.Domain.Enums;
using Academic.Domain.Events;
using Academic.Domain.Exceptions;
namespace Academic.Domain.Entities;
public sealed class Department : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = default!;
    public string Code { get; private set; } = default!;
    public string? Description { get; private set; }
    public int EstablishedYear { get; private set; }
    public Guid? HeadOfDepartmentId { get; private set; }
    public DepartmentStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Department() { }

    public static Department Create(Guid tenantId, string name, string code, int establishedYear, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new AcademicDomainException("INVALID_NAME", "Department name is required.");
        if (string.IsNullOrWhiteSpace(code)) throw new AcademicDomainException("INVALID_CODE", "Department code is required.");
        if (establishedYear < 1800 || establishedYear > DateTime.UtcNow.Year) throw new AcademicDomainException("INVALID_YEAR", "Invalid established year.");
        var dept = new Department
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name.Trim(),
            Code = code.Trim().ToUpperInvariant(),
            Description = description?.Trim(),
            EstablishedYear = establishedYear,
            Status = DepartmentStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        dept.RaiseDomainEvent(new DepartmentCreatedEvent(dept.Id, dept.TenantId, dept.Name, dept.Code));
        return dept;
    }

    public void Update(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new AcademicDomainException("INVALID_NAME", "Department name is required.");
        Name = name.Trim();
        Description = description?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignHOD(Guid hodId) { HeadOfDepartmentId = hodId; UpdatedAt = DateTime.UtcNow; }
    public void Deactivate() {
    if (Status == DepartmentStatus.Inactive)
        throw new AcademicDomainException("ALREADY_INACTIVE", "Department is already inactive.");
    Status = DepartmentStatus.Inactive;
    UpdatedAt = DateTime.UtcNow;
}
    public void Activate() { Status = DepartmentStatus.Active; UpdatedAt = DateTime.UtcNow; }
}
