using MediatR;
using Faculty.Application.DTOs;
using Faculty.Application.Interfaces;
using CourseAssignmentEntity = Faculty.Domain.Entities.CourseAssignment;
namespace Faculty.Application.CourseAssignment.Queries;
internal static class AssignmentMapper
{
    internal static CourseAssignmentDto ToDto(CourseAssignmentEntity a)
        => new(a.Id, a.FacultyId, a.CourseId, a.AcademicYear, a.Semester, a.Section, a.AssignedAt);
}
public sealed class GetFacultyCourseAssignmentsQueryHandler : IRequestHandler<GetFacultyCourseAssignmentsQuery, List<CourseAssignmentDto>>
{
    private readonly ICourseAssignmentRepository _repository;
    public GetFacultyCourseAssignmentsQueryHandler(ICourseAssignmentRepository repository) => _repository = repository;
    public async Task<List<CourseAssignmentDto>> Handle(GetFacultyCourseAssignmentsQuery query, CancellationToken ct)
    {
        var list = await _repository.GetByFacultyAsync(query.FacultyId, query.TenantId, ct);
        return list.Select(AssignmentMapper.ToDto).ToList();
    }
}
public sealed class GetFacultyCoursesByYearQueryHandler : IRequestHandler<GetFacultyCoursesByYearQuery, List<CourseAssignmentDto>>
{
    private readonly ICourseAssignmentRepository _repository;
    public GetFacultyCoursesByYearQueryHandler(ICourseAssignmentRepository repository) => _repository = repository;
    public async Task<List<CourseAssignmentDto>> Handle(GetFacultyCoursesByYearQuery query, CancellationToken ct)
    {
        var list = await _repository.GetByFacultyAndYearAsync(query.FacultyId, query.AcademicYear, query.TenantId, ct);
        return list.Select(AssignmentMapper.ToDto).ToList();
    }
}
