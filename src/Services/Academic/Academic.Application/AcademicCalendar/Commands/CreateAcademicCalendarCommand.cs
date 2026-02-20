using Academic.Application.DTOs;
using MediatR;
namespace Academic.Application.AcademicCalendar.Commands;
public sealed record CreateAcademicCalendarCommand(Guid TenantId, string AcademicYear, int Semester, DateTime StartDate, DateTime EndDate, DateTime ExamStartDate, DateTime ExamEndDate, DateTime RegistrationOpenDate, DateTime RegistrationCloseDate) : IRequest<AcademicCalendarDto>;