using Academic.Application.Interfaces;
using Academic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace Academic.Infrastructure.Persistence.Repositories;
internal sealed class AcademicCalendarRepository : IAcademicCalendarRepository
{
    private readonly AcademicDbContext _db;
    public AcademicCalendarRepository(AcademicDbContext db) => _db = db;
    public async Task<AcademicCalendar?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
        => await _db.AcademicCalendars.FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tenantId, ct);
    public async Task<AcademicCalendar?> GetActiveAsync(Guid tenantId, CancellationToken ct = default)
        => await _db.AcademicCalendars.FirstOrDefaultAsync(a => a.TenantId == tenantId && a.IsActive, ct);
    public async Task<IReadOnlyList<AcademicCalendar>> GetAllAsync(Guid tenantId, CancellationToken ct = default)
        => await _db.AcademicCalendars.Where(a => a.TenantId == tenantId).OrderByDescending(a => a.AcademicYear).ToListAsync(ct);
    public async Task AddAsync(AcademicCalendar calendar, CancellationToken ct = default)
    {
        await _db.AcademicCalendars.AddAsync(calendar, ct);
        await _db.SaveChangesAsync(ct);
    }
    public async Task UpdateAsync(AcademicCalendar calendar, CancellationToken ct = default)
    {
        _db.AcademicCalendars.Update(calendar);
        await _db.SaveChangesAsync(ct);
    }
}