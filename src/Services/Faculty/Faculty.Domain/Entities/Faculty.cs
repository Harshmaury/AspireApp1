using Faculty.Domain.Common;
using Faculty.Domain.Enums;
using Faculty.Domain.Events;
using Faculty.Domain.Exceptions;
namespace Faculty.Domain.Entities;
public sealed class Faculty : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid DepartmentId { get; private set; }
    public string EmployeeId { get; private set; } = default!;
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public Designation Designation { get; private set; }
    public string Specialization { get; private set; } = default!;
    public string HighestQualification { get; private set; } = default!;
    public int ExperienceYears { get; private set; }
    public bool IsPhD { get; private set; }
    public DateOnly JoiningDate { get; private set; }
    public FacultyStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    private Faculty() { }
    public static Faculty Create(Guid tenantId, Guid userId, Guid departmentId, string employeeId, string firstName, string lastName, string email, Designation designation, string specialization, string highestQualification, int experienceYears, bool isPhD, DateOnly joiningDate)
    {
        if (string.IsNullOrWhiteSpace(employeeId)) throw new FacultyDomainException("INVALID_EMPLOYEE_ID", "Employee ID is required.");
        if (string.IsNullOrWhiteSpace(firstName)) throw new FacultyDomainException("INVALID_NAME", "First name is required.");
        if (string.IsNullOrWhiteSpace(lastName)) throw new FacultyDomainException("INVALID_NAME", "Last name is required.");
        if (string.IsNullOrWhiteSpace(email)) throw new FacultyDomainException("INVALID_EMAIL", "Email is required.");
        if (string.IsNullOrWhiteSpace(specialization)) throw new FacultyDomainException("INVALID_SPECIALIZATION", "Specialization is required.");
        if (experienceYears < 0) throw new FacultyDomainException("INVALID_EXPERIENCE", "Experience years cannot be negative.");
        if (joiningDate > DateOnly.FromDateTime(DateTime.UtcNow)) throw new FacultyDomainException("INVALID_JOINING_DATE", "Joining date cannot be in the future.");
        var faculty = new Faculty
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            DepartmentId = departmentId,
            EmployeeId = employeeId.Trim().ToUpper(),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = email.Trim().ToLower(),
            Designation = designation,
            Specialization = specialization.Trim(),
            HighestQualification = highestQualification.Trim(),
            ExperienceYears = experienceYears,
            IsPhD = isPhD,
            JoiningDate = joiningDate,
            Status = FacultyStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        faculty.RaiseDomainEvent(new FacultyCreatedEvent(faculty.Id, tenantId, userId));
        return faculty;
    }
    public void UpdateDesignation(Designation designation)
    {
        Designation = designation;
        UpdatedAt = DateTime.UtcNow;
    }
    public void SetStatus(FacultyStatus status)
    {
        if (Status == status) throw new FacultyDomainException("SAME_STATUS", $"Faculty is already {status}.");
        Status = status;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new FacultyStatusChangedEvent(Id, TenantId, status.ToString()));
    }
    public void UpdateExperience(int years)
    {
        if (years < 0) throw new FacultyDomainException("INVALID_EXPERIENCE", "Experience cannot be negative.");
        ExperienceYears = years;
        UpdatedAt = DateTime.UtcNow;
    }
}
