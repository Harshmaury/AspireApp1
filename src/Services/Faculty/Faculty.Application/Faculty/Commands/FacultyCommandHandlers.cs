using MediatR;
using Faculty.Application.Interfaces;
using Faculty.Domain.Enums;
using Faculty.Domain.Exceptions;
using FacultyEntity = Faculty.Domain.Entities.Faculty;
namespace Faculty.Application.Faculty.Commands;
public sealed class CreateFacultyCommandHandler : IRequestHandler<CreateFacultyCommand, Guid>
{
    private readonly IFacultyRepository _repository;
    public CreateFacultyCommandHandler(IFacultyRepository repository) => _repository = repository;
    public async Task<Guid> Handle(CreateFacultyCommand cmd, CancellationToken ct)
    {
        var existing = await _repository.GetByEmployeeIdAsync(cmd.EmployeeId, cmd.TenantId, ct);
        if (existing is not null) throw new FacultyDomainException("DUPLICATE_EMPLOYEE_ID", $"Employee ID {cmd.EmployeeId} already exists.");
        var designation = Enum.Parse<Designation>(cmd.Designation, true);
        var faculty = FacultyEntity.Create(cmd.TenantId, cmd.UserId, cmd.DepartmentId, cmd.EmployeeId, cmd.FirstName, cmd.LastName, cmd.Email, designation, cmd.Specialization, cmd.HighestQualification, cmd.ExperienceYears, cmd.IsPhD, cmd.JoiningDate);
        await _repository.AddAsync(faculty, ct);
        return faculty.Id;
    }
}
public sealed class UpdateFacultyDesignationCommandHandler : IRequestHandler<UpdateFacultyDesignationCommand>
{
    private readonly IFacultyRepository _repository;
    public UpdateFacultyDesignationCommandHandler(IFacultyRepository repository) => _repository = repository;
    public async Task Handle(UpdateFacultyDesignationCommand cmd, CancellationToken ct)
    {
        var faculty = await _repository.GetByIdAsync(cmd.FacultyId, cmd.TenantId, ct)
            ?? throw new FacultyDomainException("NOT_FOUND", $"Faculty {cmd.FacultyId} not found.");
        faculty.UpdateDesignation(Enum.Parse<Designation>(cmd.Designation, true));
        await _repository.UpdateAsync(faculty, ct);
    }
}
public sealed class UpdateFacultyStatusCommandHandler : IRequestHandler<UpdateFacultyStatusCommand>
{
    private readonly IFacultyRepository _repository;
    public UpdateFacultyStatusCommandHandler(IFacultyRepository repository) => _repository = repository;
    public async Task Handle(UpdateFacultyStatusCommand cmd, CancellationToken ct)
    {
        var faculty = await _repository.GetByIdAsync(cmd.FacultyId, cmd.TenantId, ct)
            ?? throw new FacultyDomainException("NOT_FOUND", $"Faculty {cmd.FacultyId} not found.");
        faculty.SetStatus(Enum.Parse<FacultyStatus>(cmd.Status, true));
        await _repository.UpdateAsync(faculty, ct);
    }
}
public sealed class UpdateFacultyExperienceCommandHandler : IRequestHandler<UpdateFacultyExperienceCommand>
{
    private readonly IFacultyRepository _repository;
    public UpdateFacultyExperienceCommandHandler(IFacultyRepository repository) => _repository = repository;
    public async Task Handle(UpdateFacultyExperienceCommand cmd, CancellationToken ct)
    {
        var faculty = await _repository.GetByIdAsync(cmd.FacultyId, cmd.TenantId, ct)
            ?? throw new FacultyDomainException("NOT_FOUND", $"Faculty {cmd.FacultyId} not found.");
        faculty.UpdateExperience(cmd.ExperienceYears);
        await _repository.UpdateAsync(faculty, ct);
    }
}
