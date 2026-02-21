using Academic.Application.DTOs;
using Academic.Application.Interfaces;
using MediatR;
namespace Academic.Application.AcademicCalendar.Queries;
public sealed record GetAcademicCalendarByIdQuery(Guid Id, Guid TenantId) : IRequest<AcademicCalendarDto?>;
public sealed class GetAcademicCalendarByIdQueryHandler : IRequestHandler<GetAcademicCalendarByIdQuery, AcademicCalendarDto?>
{
    private readonly IAcademicCalendarRepository _repo;
    public GetAcademicCalendarByIdQueryHandler(IAcademicCalendarRepository repo) => _repo = repo;
    public async Task<AcademicCalendarDto?> Handle(GetAcademicCalendarByIdQuery req, CancellationToken ct)
    {
        var a = await _repo.GetByIdAsync(req.Id, req.TenantId, ct);
        if (a is null) return null;
        return new AcademicCalendarDto(a.Id, a.TenantId, a.AcademicYear, a.Semester, a.StartDate, a.EndDate, a.ExamStartDate, a.ExamEndDate, a.RegistrationOpenDate, a.RegistrationCloseDate, a.IsActive);
    }
}