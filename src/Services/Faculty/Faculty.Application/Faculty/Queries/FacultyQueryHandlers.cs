using MediatR;
using Faculty.Application.DTOs;
using Faculty.Application.Interfaces;
using FacultyEntity = Faculty.Domain.Entities.Faculty;
namespace Faculty.Application.Faculty.Queries;
internal static class FacultyMapper
{
    internal static FacultyDto ToDto(FacultyEntity f)
        => new(f.Id, f.TenantId, f.UserId, f.DepartmentId, f.EmployeeId, f.FirstName, f.LastName, f.Email, f.Designation.ToString(), f.Specialization, f.HighestQualification, f.ExperienceYears, f.IsPhD, f.JoiningDate, f.Status.ToString());
}
public sealed class GetFacultyByIdQueryHandler : IRequestHandler<GetFacultyByIdQuery, FacultyDto?>
{
    private readonly IFacultyRepository _repository;
    public GetFacultyByIdQueryHandler(IFacultyRepository repository) => _repository = repository;
    public async Task<FacultyDto?> Handle(GetFacultyByIdQuery query, CancellationToken ct)
    {
        var f = await _repository.GetByIdAsync(query.FacultyId, query.TenantId, ct);
        return f is null ? null : FacultyMapper.ToDto(f);
    }
}
public sealed class GetFacultyByDepartmentQueryHandler : IRequestHandler<GetFacultyByDepartmentQuery, List<FacultyDto>>
{
    private readonly IFacultyRepository _repository;
    public GetFacultyByDepartmentQueryHandler(IFacultyRepository repository) => _repository = repository;
    public async Task<List<FacultyDto>> Handle(GetFacultyByDepartmentQuery query, CancellationToken ct)
    {
        var list = await _repository.GetByDepartmentAsync(query.DepartmentId, query.TenantId, ct);
        return list.Select(FacultyMapper.ToDto).ToList();
    }
}
public sealed class GetAllFacultyQueryHandler : IRequestHandler<GetAllFacultyQuery, List<FacultyDto>>
{
    private readonly IFacultyRepository _repository;
    public GetAllFacultyQueryHandler(IFacultyRepository repository) => _repository = repository;
    public async Task<List<FacultyDto>> Handle(GetAllFacultyQuery query, CancellationToken ct)
    {
        var list = await _repository.GetAllAsync(query.TenantId, ct);
        return list.Select(FacultyMapper.ToDto).ToList();
    }
}
public sealed class GetFacultyNirfQueryHandler : IRequestHandler<GetFacultyNirfQuery, FacultyNirfDto>
{
    private readonly IFacultyRepository _facultyRepo;
    private readonly IPublicationRepository _publicationRepo;
    public GetFacultyNirfQueryHandler(IFacultyRepository facultyRepo, IPublicationRepository publicationRepo)
    {
        _facultyRepo = facultyRepo;
        _publicationRepo = publicationRepo;
    }
    public async Task<FacultyNirfDto> Handle(GetFacultyNirfQuery query, CancellationToken ct)
    {
        var allFaculty = await _facultyRepo.GetAllAsync(query.TenantId, ct);
        var phdCount = allFaculty.Count(f => f.IsPhD);
        var total = allFaculty.Count;
        var phdPct = total == 0 ? 0m : Math.Round((decimal)phdCount / total * 100, 2);
        var sciPubs = await _publicationRepo.GetByTypeAsync("SCI", query.TenantId, ct);
        var scopusPubs = await _publicationRepo.GetByTypeAsync("Scopus", query.TenantId, ct);
        var allPubs = await _publicationRepo.GetByTypeAsync("", query.TenantId, ct);
        return new FacultyNirfDto(query.TenantId, total, phdCount, phdPct, allPubs.Count, sciPubs.Count, scopusPubs.Count);
    }
}
