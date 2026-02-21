using MediatR;
using Faculty.Application.Interfaces;
using Faculty.Domain.Exceptions;
using CourseAssignmentEntity = Faculty.Domain.Entities.CourseAssignment;
namespace Faculty.Application.CourseAssignment.Commands;
public sealed class AssignCourseCommandHandler : IRequestHandler<AssignCourseCommand, Guid>
{
    private readonly ICourseAssignmentRepository _repository;
    public AssignCourseCommandHandler(ICourseAssignmentRepository repository) => _repository = repository;
    public async Task<Guid> Handle(AssignCourseCommand cmd, CancellationToken ct)
    {
        var exists = await _repository.ExistsAsync(cmd.FacultyId, cmd.CourseId, cmd.AcademicYear, cmd.TenantId, ct);
        if (exists) throw new FacultyDomainException("DUPLICATE_ASSIGNMENT", "This course is already assigned to this faculty for the given year.");
        var assignment = CourseAssignmentEntity.Create(cmd.TenantId, cmd.FacultyId, cmd.CourseId, cmd.AcademicYear, cmd.Semester, cmd.Section);
        await _repository.AddAsync(assignment, ct);
        return assignment.Id;
    }
}
public sealed class UnassignCourseCommandHandler : IRequestHandler<UnassignCourseCommand>
{
    private readonly ICourseAssignmentRepository _repository;
    public UnassignCourseCommandHandler(ICourseAssignmentRepository repository) => _repository = repository;
    public async Task Handle(UnassignCourseCommand cmd, CancellationToken ct)
        => await _repository.DeleteAsync(cmd.AssignmentId, cmd.TenantId, ct);
}
