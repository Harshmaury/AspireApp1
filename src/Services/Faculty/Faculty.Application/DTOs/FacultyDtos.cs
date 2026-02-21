namespace Faculty.Application.DTOs;
public sealed record FacultyDto(Guid Id, Guid TenantId, Guid UserId, Guid DepartmentId, string EmployeeId, string FirstName, string LastName, string Email, string Designation, string Specialization, string HighestQualification, int ExperienceYears, bool IsPhD, DateOnly JoiningDate, string Status);
public sealed record CourseAssignmentDto(Guid Id, Guid FacultyId, Guid CourseId, string AcademicYear, int Semester, string? Section, DateTime AssignedAt);
public sealed record PublicationDto(Guid Id, Guid FacultyId, string Title, string Journal, int PublishedYear, string Type, string? DOI, int CitationCount);
public sealed record FacultyNirfDto(Guid TenantId, int TotalFaculty, int PhdCount, decimal PhdPercentage, int TotalPublications, int SciPublications, int ScopusPublications);
