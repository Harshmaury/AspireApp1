using MediatR;
using Examination.Application.Interfaces;
using MarksEntryEntity = Examination.Domain.Entities.MarksEntry;
namespace Examination.Application.MarksEntry.Commands;
public sealed class EnterMarksCommandHandler : IRequestHandler<EnterMarksCommand, Guid>
{
    private readonly IMarksEntryRepository _repository;
    public EnterMarksCommandHandler(IMarksEntryRepository repository) => _repository = repository;
    public async Task<Guid> Handle(EnterMarksCommand cmd, CancellationToken ct)
    {
        var entry = MarksEntryEntity.Create(cmd.TenantId, cmd.StudentId, cmd.ExamScheduleId, cmd.CourseId, cmd.MarksObtained, cmd.MaxMarks, cmd.IsAbsent, cmd.EnteredBy);
        await _repository.AddAsync(entry, ct);
        return entry.Id;
    }
}
