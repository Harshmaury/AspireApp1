using MediatR;
using Attendance.Application.DTOs;
using Attendance.Application.Interfaces;
namespace Attendance.Application.Condonation.Queries;
internal static class CondonationMapper
{
    internal static CondonationRequestDto ToDto(Attendance.Domain.Entities.CondonationRequest r)
        => new(r.Id, r.StudentId, r.CourseId, r.Reason, r.DocumentUrl, r.Status.ToString(), r.ReviewNote, r.CreatedAt, r.ReviewedAt);
}
public sealed class GetPendingCondonationsQueryHandler : IRequestHandler<GetPendingCondonationsQuery, List<CondonationRequestDto>>
{
    private readonly ICondonationRequestRepository _repository;
    public GetPendingCondonationsQueryHandler(ICondonationRequestRepository repository) => _repository = repository;
    public async Task<List<CondonationRequestDto>> Handle(GetPendingCondonationsQuery query, CancellationToken ct)
    {
        var requests = await _repository.GetPendingAsync(query.TenantId, ct);
        return requests.Select(CondonationMapper.ToDto).ToList();
    }
}
public sealed class GetStudentCondonationsQueryHandler : IRequestHandler<GetStudentCondonationsQuery, List<CondonationRequestDto>>
{
    private readonly ICondonationRequestRepository _repository;
    public GetStudentCondonationsQueryHandler(ICondonationRequestRepository repository) => _repository = repository;
    public async Task<List<CondonationRequestDto>> Handle(GetStudentCondonationsQuery query, CancellationToken ct)
    {
        var requests = await _repository.GetByStudentAsync(query.StudentId, query.TenantId, ct);
        return requests.Select(CondonationMapper.ToDto).ToList();
    }
}
