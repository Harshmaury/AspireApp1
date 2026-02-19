using Identity.Domain.Entities;

namespace Identity.Application.Interfaces;

public interface ITenantRepository
{
    Task<Tenant?> FindBySlugAsync(string slug, CancellationToken ct = default);
    Task<bool> ExistsAsync(string slug, CancellationToken ct = default);
}
