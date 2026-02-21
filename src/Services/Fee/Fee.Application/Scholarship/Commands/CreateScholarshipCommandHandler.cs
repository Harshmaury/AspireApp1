using MediatR;
using Fee.Application.Interfaces;
using ScholarshipEntity = Fee.Domain.Entities.Scholarship;
namespace Fee.Application.Scholarship.Commands;
public sealed class CreateScholarshipCommandHandler : IRequestHandler<CreateScholarshipCommand, Guid>
{
    private readonly IScholarshipRepository _repository;
    public CreateScholarshipCommandHandler(IScholarshipRepository repository) => _repository = repository;
    public async Task<Guid> Handle(CreateScholarshipCommand cmd, CancellationToken ct)
    {
        var scholarship = ScholarshipEntity.Create(cmd.TenantId, cmd.StudentId, cmd.Name, cmd.Amount, cmd.AcademicYear);
        await _repository.AddAsync(scholarship, ct);
        return scholarship.Id;
    }
}
