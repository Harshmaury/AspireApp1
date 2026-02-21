using Hostel.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using HostelEntity = Hostel.Domain.Entities.Hostel;
namespace Hostel.Infrastructure.Persistence.Repositories;
public sealed class HostelRepository(HostelDbContext db) : IHostelRepository
{
    public Task<HostelEntity?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default) =>
        db.Hostels.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);
    public Task<List<HostelEntity>> GetAllAsync(Guid tenantId, int page, int pageSize, CancellationToken ct = default) =>
        db.Hostels.Where(x => x.TenantId == tenantId)
            .OrderBy(x => x.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
    public Task<int> CountAsync(Guid tenantId, CancellationToken ct = default) =>
        db.Hostels.CountAsync(x => x.TenantId == tenantId, ct);
    public async Task AddAsync(HostelEntity hostel, CancellationToken ct = default) =>
        await db.Hostels.AddAsync(hostel, ct);
    public void Update(HostelEntity hostel) => db.Hostels.Update(hostel);
}
