using Microsoft.EntityFrameworkCore;
using Attendance.Application.Interfaces;
using Attendance.Domain.Entities;
using Attendance.Domain.Exceptions;
using Attendance.Infrastructure.Persistence;
namespace Attendance.Infrastructure.Persistence.Repositories;
public sealed class CondonationRequestRepository : ICondonationRequestRepository
{
    private readonly AttendanceDbContext _db;
    public CondonationRequestRepository(AttendanceDbContext db) => _db = db;
    public async Task<CondonationRequest?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
        => await _db.CondonationRequests.FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId, ct);
    public async Task<List<CondonationRequest>> GetByStudentAsync(Guid studentId, Guid tenantId, CancellationToken ct = default)
        => await _db.CondonationRequests.Where(r => r.StudentId == studentId && r.TenantId == tenantId).ToListAsync(ct);
    public async Task<List<CondonationRequest>> GetPendingAsync(Guid tenantId, CancellationToken ct = default)
        => await _db.CondonationRequests.Where(r => r.TenantId == tenantId && r.Status == Attendance.Domain.Enums.CondonationStatus.Pending).ToListAsync(ct);
    public async Task AddAsync(CondonationRequest request, CancellationToken ct = default)
    {
        await _db.CondonationRequests.AddAsync(request, ct);
        await _db.SaveChangesAsync(ct);
    }
    public async Task UpdateAsync(CondonationRequest request, CancellationToken ct = default)
    {
        _db.CondonationRequests.Update(request);
        await _db.SaveChangesAsync(ct);
    }
}
