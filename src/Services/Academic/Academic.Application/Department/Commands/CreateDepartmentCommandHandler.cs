using Academic.Application.DTOs;
using Academic.Application.Interfaces;
using Academic.Domain.Exceptions;
using MediatR;
namespace Academic.Application.Department.Commands;
public sealed class CreateDepartmentCommandHandler : IRequestHandler<CreateDepartmentCommand, DepartmentDto>
{
    private readonly IDepartmentRepository _repo;
    public CreateDepartmentCommandHandler(IDepartmentRepository repo) => _repo = repo;
    public async Task<DepartmentDto> Handle(CreateDepartmentCommand cmd, CancellationToken ct)
    {
        if (await _repo.ExistsAsync(cmd.Code, cmd.TenantId, ct))
            throw new AcademicDomainException("DUPLICATE_CODE", $"Department with code '{cmd.Code}' already exists.");
        var dept = Academic.Domain.Entities.Department.Create(cmd.TenantId, cmd.Name, cmd.Code, cmd.EstablishedYear, cmd.Description);
        await _repo.AddAsync(dept, ct);
        return new DepartmentDto(dept.Id, dept.TenantId, dept.Name, dept.Code, dept.Description, dept.EstablishedYear, dept.Status.ToString(), dept.CreatedAt);
    }
}