using Academic.Application.DTOs;
using Academic.Application.Interfaces;
using MediatR;
namespace Academic.Application.AcademicCalendar.Commands;
public sealed class CreateAcademicCalendarCommandHandler : IRequestHandler<CreateAcademicCalendarCommand, AcademicCalendarDto>
{
    private readonly IAcademicCalendarRepository _repo;
    public CreateAcademicCalendarCommandHandler(IAcademicCalendarRepository repo) => _repo = repo;
    public async Task<AcademicCalendarDto> Handle(CreateAcademicCalendarCommand cmd, CancellationToken ct)
    {
        var cal = Academic.Domain.Entities.AcademicCalendar.Create(cmd.TenantId, cmd.AcademicYear, cmd.Semester, cmd.StartDate, cmd.EndDate, cmd.ExamStartDate, cmd.ExamEndDate, cmd.RegistrationOpenDate, cmd.RegistrationCloseDate);
        await _repo.AddAsync(cal, ct);
        return new AcademicCalendarDto(cal.Id, cal.TenantId, cal.AcademicYear, cal.Semester, cal.StartDate, cal.EndDate, cal.ExamStartDate, cal.ExamEndDate, cal.RegistrationOpenDate, cal.RegistrationCloseDate, cal.IsActive);
    }
}