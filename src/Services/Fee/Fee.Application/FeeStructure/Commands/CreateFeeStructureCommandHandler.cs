using MediatR;
using Fee.Application.Interfaces;
using FeeStructureEntity = Fee.Domain.Entities.FeeStructure;
namespace Fee.Application.FeeStructure.Commands;
public sealed class CreateFeeStructureCommandHandler : IRequestHandler<CreateFeeStructureCommand, Guid>
{
    private readonly IFeeStructureRepository _repository;
    public CreateFeeStructureCommandHandler(IFeeStructureRepository repository) => _repository = repository;
    public async Task<Guid> Handle(CreateFeeStructureCommand cmd, CancellationToken ct)
    {
        var feeStructure = FeeStructureEntity.Create(cmd.TenantId, cmd.ProgrammeId, cmd.AcademicYear, cmd.Semester, cmd.TuitionFee, cmd.ExamFee, cmd.DevelopmentFee, cmd.MedicalFee, cmd.DueDate, cmd.HostelFee, cmd.MessFee);
        await _repository.AddAsync(feeStructure, ct);
        return feeStructure.Id;
    }
}
