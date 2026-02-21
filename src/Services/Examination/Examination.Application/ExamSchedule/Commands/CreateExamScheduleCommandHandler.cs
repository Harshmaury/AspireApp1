using MediatR;
using Examination.Application.Interfaces;
using ExamScheduleEntity = Examination.Domain.Entities.ExamSchedule;
using Examination.Domain.Enums;
namespace Examination.Application.ExamSchedule.Commands;
public sealed class CreateExamScheduleCommandHandler : IRequestHandler<CreateExamScheduleCommand, Guid>
{
    private readonly IExamScheduleRepository _repository;
    public CreateExamScheduleCommandHandler(IExamScheduleRepository repository) => _repository = repository;
    public async Task<Guid> Handle(CreateExamScheduleCommand cmd, CancellationToken ct)
    {
        var examType = Enum.Parse<ExamType>(cmd.ExamType, true);
        var schedule = ExamScheduleEntity.Create(cmd.TenantId, cmd.CourseId, cmd.AcademicYear, cmd.Semester, examType, cmd.ExamDate, cmd.Duration, cmd.Venue, cmd.MaxMarks, cmd.PassingMarks);
        await _repository.AddAsync(schedule, ct);
        return schedule.Id;
    }
}
