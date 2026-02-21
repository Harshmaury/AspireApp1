using MediatR;
namespace Examination.Application.ExamSchedule.Commands;
public sealed record CreateExamScheduleCommand(
    Guid TenantId,
    Guid CourseId,
    string AcademicYear,
    int Semester,
    string ExamType,
    DateTime ExamDate,
    int Duration,
    string Venue,
    int MaxMarks,
    int PassingMarks) : IRequest<Guid>;
