namespace Notification.Application.Events;

// ── Identity ─────────────────────────────────────────────────────────────────
public sealed record UserRegisteredEvent(
    Guid UserId, Guid TenantId, string Email,
    string FirstName, string LastName, string Role);

public sealed record TenantCreatedEvent(
    Guid TenantId, string Name, string AdminEmail);

// ── Student — shapes match Student.Domain.Events exactly ─────────────────────
// StudentCreatedEvent: fired when a student profile is first created
public sealed record StudentCreatedEvent(
    Guid StudentId, Guid TenantId, Guid UserId,
    string FirstName, string LastName, string Email);

// StudentStatusChangedEvent: fired on Admit/Enroll/Suspend/Reinstate/Graduate/Archive
public sealed record StudentStatusChangedEvent(
    Guid StudentId, Guid TenantId,
    string OldStatus, string NewStatus,
    string Email, string FirstName);

// ── Academic ─────────────────────────────────────────────────────────────────
public sealed record AcademicCalendarPublishedEvent(
    Guid TenantId, string AcademicYear,
    int Semester, DateTime StartDate, DateTime EndDate);

public sealed record CoursePublishedEvent(
    Guid TenantId, Guid CourseId, string CourseName, string CourseCode);

// ── Examination — no Email in domain events; consumer uses null guard ─────────
public sealed record ResultDeclaredEvent(
    Guid StudentId, Guid TenantId,
    string AcademicYear, int Semester, decimal SGPA, decimal CGPA);

public sealed record MarksPublishedEvent(
    Guid ExamScheduleId, Guid TenantId);

public sealed record StudentBacklogEvent(
    Guid StudentId, Guid TenantId, Guid CourseId);

// ── Fee — no Email in domain events; consumer uses null guard ─────────────────
public sealed record FeePaymentReceivedEvent(
    Guid PaymentId, Guid StudentId, Guid TenantId, decimal AmountPaid);

public sealed record FeeDefaulterMarkedEvent(
    Guid StudentId, Guid TenantId, string AcademicYear);

public sealed record ScholarshipGrantedEvent(
    Guid ScholarshipId, Guid StudentId, Guid TenantId, decimal Amount);