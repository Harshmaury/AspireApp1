using Academic.Application.DTOs;
using MediatR;
namespace Academic.Application.AcademicCalendar.Queries;
public sealed record GetActiveCalendarQuery(Guid TenantId) : IRequest<AcademicCalendarDto?>;