namespace Examination.Application.DTOs;
public sealed record ExamScheduleDto(Guid Id, Guid CourseId, string AcademicYear, int Semester, string ExamType, DateTime ExamDate, int Duration, string Venue, int MaxMarks, int PassingMarks, string Status);
public sealed record MarksEntryDto(Guid Id, Guid StudentId, Guid CourseId, decimal MarksObtained, string Grade, decimal GradePoint, bool IsAbsent, string Status);
public sealed record ResultCardDto(Guid Id, Guid StudentId, string AcademicYear, int Semester, decimal SGPA, decimal CGPA, int TotalCreditsEarned, int TotalCreditsAttempted, bool HasBacklog, DateTime? PublishedAt);
public sealed record HallTicketDto(Guid Id, Guid StudentId, Guid ExamScheduleId, string RollNumber, string SeatNumber, bool IsEligible, string? IneligibilityReason, DateTime IssuedAt);
