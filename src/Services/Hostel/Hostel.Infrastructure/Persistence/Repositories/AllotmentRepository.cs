using Hostel.Application.Interfaces;
using Hostel.Domain.Entities;
using Hostel.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using UMS.SharedKernel.Tenancy;

namespace Hostel.Infrastructure.Persistence.Repositories;

internal sealed class AllotmentRepository : IAllotmentRepository
{
    private readonly HostelDbContext _db;
    public AllotmentRepository(HostelDbContext db, ITenantContext? tenant = null) => _db = db;

    public Task<RoomAllotment?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default) =>
        _db.RoomAllotments.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

    public Task<RoomAllotment?> GetActiveByStudentAsync(Guid studentId, string academicYear, Guid tenantId, CancellationToken ct = default) =>
        _db.RoomAllotments.FirstOrDefaultAsync(x =>
            x.StudentId == studentId && x.AcademicYear == academicYear &&
            x.TenantId == tenantId && x.Status == AllotmentStatus.Active, ct);

    public Task<List<RoomAllotment>> GetByRoomAsync(Guid roomId, Guid tenantId, string academicYear, CancellationToken ct = default) =>
        _db.RoomAllotments.Where(x => x.RoomId == roomId && x.TenantId == tenantId && x.AcademicYear == academicYear).ToListAsync(ct);

    public Task<int> CountActiveByRoomAsync(Guid roomId, Guid tenantId, string academicYear, CancellationToken ct = default) =>
        _db.RoomAllotments.CountAsync(x =>
            x.RoomId == roomId && x.TenantId == tenantId &&
            x.AcademicYear == academicYear && x.Status == AllotmentStatus.Active, ct);

    public Task<List<RoomAllotment>> GetAllAsync(Guid tenantId, int page, int pageSize, CancellationToken ct = default) =>
        _db.RoomAllotments.Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.AllottedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

    public Task<int> CountAsync(Guid tenantId, CancellationToken ct = default) =>
        _db.RoomAllotments.CountAsync(x => x.TenantId == tenantId, ct);

    public async Task AddAsync(RoomAllotment allotment, CancellationToken ct = default) =>
        await _db.RoomAllotments.AddAsync(allotment, ct);

    public void Update(RoomAllotment allotment) => _db.RoomAllotments.Update(allotment);
}

