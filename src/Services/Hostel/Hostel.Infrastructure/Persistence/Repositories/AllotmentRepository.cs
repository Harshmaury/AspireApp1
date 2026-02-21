using Hostel.Application.Interfaces;
using Hostel.Domain.Entities;
using Hostel.Domain.Enums;
using Microsoft.EntityFrameworkCore;
namespace Hostel.Infrastructure.Persistence.Repositories;
public sealed class AllotmentRepository(HostelDbContext db) : IAllotmentRepository
{
    public Task<RoomAllotment?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default) =>
        db.RoomAllotments.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);
    public Task<RoomAllotment?> GetActiveByStudentAsync(Guid studentId, string academicYear, Guid tenantId, CancellationToken ct = default) =>
        db.RoomAllotments.FirstOrDefaultAsync(x => x.StudentId == studentId && x.AcademicYear == academicYear
            && x.TenantId == tenantId && x.Status == AllotmentStatus.Active, ct);
    public Task<List<RoomAllotment>> GetByRoomAsync(Guid roomId, Guid tenantId, string academicYear, CancellationToken ct = default) =>
        db.RoomAllotments.Where(x => x.RoomId == roomId && x.TenantId == tenantId && x.AcademicYear == academicYear).ToListAsync(ct);
    public Task<int> CountActiveByRoomAsync(Guid roomId, Guid tenantId, string academicYear, CancellationToken ct = default) =>
        db.RoomAllotments.CountAsync(x => x.RoomId == roomId && x.TenantId == tenantId
            && x.AcademicYear == academicYear && x.Status == AllotmentStatus.Active, ct);
    public Task<List<RoomAllotment>> GetAllAsync(Guid tenantId, int page, int pageSize, CancellationToken ct = default) =>
        db.RoomAllotments.Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.AllottedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
    public Task<int> CountAsync(Guid tenantId, CancellationToken ct = default) =>
        db.RoomAllotments.CountAsync(x => x.TenantId == tenantId, ct);
    public async Task AddAsync(RoomAllotment allotment, CancellationToken ct = default) =>
        await db.RoomAllotments.AddAsync(allotment, ct);
    public void Update(RoomAllotment allotment) => db.RoomAllotments.Update(allotment);
}
