using Academic.Application.DTOs;
using Academic.Application.Interfaces;
using MediatR;
namespace Academic.Application.AcademicCalendar.Queries;
public sealed record GetAllAcademicCalendarsQuery(Guid TenantId) : IRequest<IReadOnlyList<AcademicCalendarDto>>;
public sealed class GetAllAcademicCalendarsQueryHandler : IRequestHandler<GetAllAcademicCalendarsQuery, IReadOnlyList<AcademicCalendarDto>>
{
    private readonly IAcademicCalendarRepository _repo;
    public GetAllAcademicCalendarsQueryHandler(IAcademicCalendarRepository repo) => _repo = repo;
    public async Task<IReadOnlyList<AcademicCalendarDto>> Handle(GetAllAcademicCalendarsQuery req, CancellationToken ct)
    {
        var items = await _repo.GetAllAsync(req.TenantId, ct);
        return items.Select(a => new AcademicCalendarDto(a.Id, a.TenantId, a.AcademicYear, a.Semester, a.StartDate, a.EndDate, a.ExamStartDate, a.ExamEndDate, a.RegistrationOpenDate, a.RegistrationCloseDate, a.IsActive)).ToList();
    }
}