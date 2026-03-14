// UMS — University Management System
// Key:     UMS-IDENTITY-P2-002
// Service: Identity
// Layer:   Application / Interfaces
namespace Identity.Application.Interfaces;

using Identity.Domain.Entities;

public interface ITenantRepository
{
    Task<Tenant?> FindByIdAsync(Guid tenantId, CancellationToken ct = default);
    Task<Tenant?> FindBySlugAsync(string slug, CancellationToken ct = default);
    Task<bool>    ExistsAsync(string slug, CancellationToken ct = default);
    Task          AddAsync(Tenant tenant, CancellationToken ct = default);
    Task          UpdateAsync(Tenant tenant, CancellationToken ct = default);
}
