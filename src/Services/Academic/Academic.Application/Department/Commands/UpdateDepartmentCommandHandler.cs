using Academic.Application.Interfaces;
using Academic.Domain.Exceptions;
using MediatR;
namespace Academic.Application.Department.Commands;
public sealed class UpdateDepartmentCommandHandler : IRequestHandler<UpdateDepartmentCommand>
{
    private readonly IDepartmentRepository _repo;
    public UpdateDepartmentCommandHandler(IDepartmentRepository repo) => _repo = repo;
    public async Task Handle(UpdateDepartmentCommand cmd, CancellationToken ct)
    {
        var dept = await _repo.GetByIdAsync(cmd.Id, cmd.TenantId, ct)
            ?? throw new AcademicDomainException("NOT_FOUND", $"Department '{cmd.Id}' not found.");
        dept.Update(cmd.Name, cmd.Description);
        await _repo.UpdateAsync(dept, ct);
    }
}