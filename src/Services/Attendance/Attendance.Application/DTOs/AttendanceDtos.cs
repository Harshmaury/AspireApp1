namespace Attendance.Application.DTOs;
public sealed record AttendanceRecordDto(Guid Id, Guid StudentId, Guid CourseId, string AcademicYear, int Semester, DateOnly Date, string ClassType, bool IsPresent, bool IsLocked);
public sealed record AttendanceSummaryDto(Guid Id, Guid StudentId, Guid CourseId, int TotalClasses, int AttendedClasses, decimal Percentage, bool IsShortage, bool IsWarning, bool IsEligibleForExam);
public sealed record CondonationRequestDto(Guid Id, Guid StudentId, Guid CourseId, string Reason, string? DocumentUrl, string Status, string? ReviewNote, DateTime CreatedAt, DateTime? ReviewedAt);
