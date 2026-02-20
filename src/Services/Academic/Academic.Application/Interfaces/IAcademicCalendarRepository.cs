namespace Academic.Application.Interfaces;
public interface IAcademicCalendarRepository
{
    Task<Academic.Domain.Entities.AcademicCalendar?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<Academic.Domain.Entities.AcademicCalendar?> GetActiveAsync(Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<Academic.Domain.Entities.AcademicCalendar>> GetAllAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(Academic.Domain.Entities.AcademicCalendar calendar, CancellationToken ct = default);
    Task UpdateAsync(Academic.Domain.Entities.AcademicCalendar calendar, CancellationToken ct = default);
}