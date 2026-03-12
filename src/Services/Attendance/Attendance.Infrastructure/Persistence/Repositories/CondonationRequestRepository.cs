using Attendance.Application.Interfaces;
using Attendance.Domain.Entities;
using Attendance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using UMS.SharedKernel.Tenancy;

namespace Attendance.Infrastructure.Persistence.Repositories;

internal sealed class CondonationRequestRepository : ICondonationRequestRepository
{
    private readonly AttendanceDbContext _db;

    // ITenantContext injected for AGS-007 compliance â€” filtering handled
    // by AttendanceDbContext.HasQueryFilter + explicit tenantId guards below.
    public CondonationRequestRepository(AttendanceDbContext db, ITenantContext? tenant = null)
        => _db = db;

    public async Task<CondonationRequest?> GetByIdAsync(
        Guid id, Guid tenantId, CancellationToken ct = default)
        => await _db.CondonationRequests
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId, ct);

    public async Task<List<CondonationRequest>> GetByStudentAsync(
        Guid studentId, Guid tenantId, CancellationToken ct = default)
        => await _db.CondonationRequests
            .Where(r => r.StudentId == studentId && r.TenantId == tenantId)
            .ToListAsync(ct);

    public async Task<List<CondonationRequest>> GetPendingAsync(
        Guid tenantId, CancellationToken ct = default)
        => await _db.CondonationRequests
            .Where(r => r.TenantId == tenantId &&
                        r.Status == Attendance.Domain.Enums.CondonationStatus.Pending)
            .ToListAsync(ct);

    // No SaveChangesAsync â€” callers flush via IAttendanceUnitOfWork or their
    // own DbContext scope. Calling SaveChanges inside a repository breaks the
    // Unit of Work pattern and risks partial commits.
    public async Task AddAsync(CondonationRequest request, CancellationToken ct = default)
        => await _db.CondonationRequests.AddAsync(request, ct);

    public Task UpdateAsync(CondonationRequest request, CancellationToken ct = default)
    {
        _db.CondonationRequests.Update(request);
        return Task.CompletedTask;
    }
}

