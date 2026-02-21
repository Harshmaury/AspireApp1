using MediatR;
using Attendance.Application.DTOs;
using Attendance.Application.Interfaces;
namespace Attendance.Application.AttendanceSummary.Queries;
internal static class SummaryMapper
{
    internal static AttendanceSummaryDto ToDto(Attendance.Domain.Entities.AttendanceSummary s)
        => new(s.Id, s.StudentId, s.CourseId, s.TotalClasses, s.AttendedClasses, s.Percentage, s.IsShortage, s.IsWarning, s.IsEligibleForExam());
}
public sealed class GetStudentSummaryQueryHandler : IRequestHandler<GetStudentSummaryQuery, List<AttendanceSummaryDto>>
{
    private readonly IAttendanceSummaryRepository _repository;
    public GetStudentSummaryQueryHandler(IAttendanceSummaryRepository repository) => _repository = repository;
    public async Task<List<AttendanceSummaryDto>> Handle(GetStudentSummaryQuery query, CancellationToken ct)
    {
        var summaries = await _repository.GetByStudentAsync(query.StudentId, query.TenantId, ct);
        return summaries.Select(SummaryMapper.ToDto).ToList();
    }
}
public sealed class GetStudentCourseSummaryQueryHandler : IRequestHandler<GetStudentCourseSummaryQuery, AttendanceSummaryDto?>
{
    private readonly IAttendanceSummaryRepository _repository;
    public GetStudentCourseSummaryQueryHandler(IAttendanceSummaryRepository repository) => _repository = repository;
    public async Task<AttendanceSummaryDto?> Handle(GetStudentCourseSummaryQuery query, CancellationToken ct)
    {
        var s = await _repository.GetByStudentCourseAsync(query.StudentId, query.CourseId, query.TenantId, ct);
        return s is null ? null : SummaryMapper.ToDto(s);
    }
}
public sealed class GetShortageListQueryHandler : IRequestHandler<GetShortageListQuery, List<AttendanceSummaryDto>>
{
    private readonly IAttendanceSummaryRepository _repository;
    public GetShortageListQueryHandler(IAttendanceSummaryRepository repository) => _repository = repository;
    public async Task<List<AttendanceSummaryDto>> Handle(GetShortageListQuery query, CancellationToken ct)
    {
        var summaries = await _repository.GetShortagesAsync(query.TenantId, ct);
        return summaries.Select(SummaryMapper.ToDto).ToList();
    }
}
