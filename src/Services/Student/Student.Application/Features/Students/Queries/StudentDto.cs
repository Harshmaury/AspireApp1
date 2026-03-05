namespace Student.Application.Features.Students.Queries;

public sealed record StudentDto(
    Guid      Id,
    Guid      TenantId,
    Guid      UserId,
    string    FirstName,
    string    LastName,
    string    Email,
    string    StudentNumber,
    string    Status,
    DateTime  CreatedAt,
    DateTime? UpdatedAt,
    DateTime? AdmittedAt,
    DateTime? EnrolledAt,
    DateTime? GraduatedAt,
    string?   SuspensionReason);
