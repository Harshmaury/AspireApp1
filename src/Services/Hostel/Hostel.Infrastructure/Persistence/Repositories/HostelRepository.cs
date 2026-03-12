using Hostel.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using UMS.SharedKernel.Tenancy;
using HostelEntity = Hostel.Domain.Entities.Hostel;

namespace Hostel.Infrastructure.Persistence.Repositories;

internal sealed class HostelRepository : IHostelRepository
{
    private readonly HostelDbContext _db;
    public HostelRepository(HostelDbContext db, ITenantContext? tenant = null) => _db = db;

    public Task<HostelEntity?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default) =>
        _db.Hostels.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

    public Task<List<HostelEntity>> GetAllAsync(Guid tenantId, int page, int pageSize, CancellationToken ct = default) =>
        _db.Hostels.Where(x => x.TenantId == tenantId)
            .OrderBy(x => x.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

    public Task<int> CountAsync(Guid tenantId, CancellationToken ct = default) =>
        _db.Hostels.CountAsync(x => x.TenantId == tenantId, ct);

    public async Task AddAsync(HostelEntity hostel, CancellationToken ct = default) =>
        await _db.Hostels.AddAsync(hostel, ct);

    public void Update(HostelEntity hostel) => _db.Hostels.Update(hostel);
}

