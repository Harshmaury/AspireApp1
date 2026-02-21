namespace Notification.Application.Events;

// Identity Events
public sealed record UserRegisteredEvent(Guid UserId, Guid TenantId, string Email, string FirstName, string LastName, string Role);
public sealed record TenantCreatedEvent(Guid TenantId, string Name, string AdminEmail);

// Student Events
public sealed record StudentEnrolledEvent(Guid StudentId, Guid TenantId, string Email, string FirstName, string StudentNumber);
public sealed record StudentSuspendedEvent(Guid StudentId, Guid TenantId, string Email, string FirstName);
public sealed record StudentGraduatedEvent(Guid StudentId, Guid TenantId, string Email, string FirstName);

// Academic Events
public sealed record AcademicCalendarPublishedEvent(Guid TenantId, string AcademicYear, int Semester, DateTime StartDate, DateTime EndDate);
public sealed record CoursePublishedEvent(Guid TenantId, Guid CourseId, string CourseName, string CourseCode);

// Examination Events
public sealed record ResultDeclaredEvent(Guid StudentId, Guid TenantId, string AcademicYear, int Semester, decimal SGPA, decimal CGPA);
public sealed record MarksPublishedEvent(Guid ExamScheduleId, Guid TenantId);
public sealed record StudentBacklogEvent(Guid StudentId, Guid TenantId, Guid CourseId);

// Fee Events
public sealed record FeePaymentReceivedEvent(Guid PaymentId, Guid StudentId, Guid TenantId, decimal AmountPaid);
public sealed record FeeDefaulterMarkedEvent(Guid StudentId, Guid TenantId, string AcademicYear);
public sealed record ScholarshipGrantedEvent(Guid ScholarshipId, Guid StudentId, Guid TenantId, decimal Amount);
