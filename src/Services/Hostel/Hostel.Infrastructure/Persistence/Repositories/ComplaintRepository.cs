using Hostel.Application.Interfaces;
using Hostel.Domain.Entities;
using Hostel.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using UMS.SharedKernel.Tenancy;

namespace Hostel.Infrastructure.Persistence.Repositories;

internal sealed class ComplaintRepository : IComplaintRepository
{
    private readonly HostelDbContext _db;
    public ComplaintRepository(HostelDbContext db, ITenantContext? tenant = null) => _db = db;

    public Task<HostelComplaint?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default) =>
        _db.HostelComplaints.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

    public Task<List<HostelComplaint>> GetAllAsync(Guid tenantId, ComplaintStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        var q = _db.HostelComplaints.Where(x => x.TenantId == tenantId);
        if (status.HasValue) q = q.Where(x => x.Status == status.Value);
        return q.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
    }

    public Task<int> CountAsync(Guid tenantId, ComplaintStatus? status, CancellationToken ct = default)
    {
        var q = _db.HostelComplaints.Where(x => x.TenantId == tenantId);
        if (status.HasValue) q = q.Where(x => x.Status == status.Value);
        return q.CountAsync(ct);
    }

    public Task<List<HostelComplaint>> GetByStudentAsync(Guid studentId, Guid tenantId, CancellationToken ct = default) =>
        _db.HostelComplaints.Where(x => x.StudentId == studentId && x.TenantId == tenantId)
            .OrderByDescending(x => x.CreatedAt).ToListAsync(ct);

    public async Task AddAsync(HostelComplaint complaint, CancellationToken ct = default) =>
        await _db.HostelComplaints.AddAsync(complaint, ct);

    public void Update(HostelComplaint complaint) => _db.HostelComplaints.Update(complaint);
}

