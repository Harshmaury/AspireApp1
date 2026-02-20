using Academic.Application.DTOs;
using Academic.Application.Interfaces;
using MediatR;
namespace Academic.Application.AcademicCalendar.Queries;
public sealed class GetActiveCalendarQueryHandler : IRequestHandler<GetActiveCalendarQuery, AcademicCalendarDto?>
{
    private readonly IAcademicCalendarRepository _repo;
    public GetActiveCalendarQueryHandler(IAcademicCalendarRepository repo) => _repo = repo;
    public async Task<AcademicCalendarDto?> Handle(GetActiveCalendarQuery query, CancellationToken ct)
    {
        var cal = await _repo.GetActiveAsync(query.TenantId, ct);
        return cal is null ? null : new AcademicCalendarDto(cal.Id, cal.TenantId, cal.AcademicYear, cal.Semester, cal.StartDate, cal.EndDate, cal.ExamStartDate, cal.ExamEndDate, cal.RegistrationOpenDate, cal.RegistrationCloseDate, cal.IsActive);
    }
}