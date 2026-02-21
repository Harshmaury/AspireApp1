using MediatR;
namespace Examination.Application.MarksEntry.Commands;
public sealed record EnterMarksCommand(
    Guid TenantId,
    Guid StudentId,
    Guid ExamScheduleId,
    Guid CourseId,
    decimal MarksObtained,
    int MaxMarks,
    bool IsAbsent,
    Guid EnteredBy) : IRequest<Guid>;
