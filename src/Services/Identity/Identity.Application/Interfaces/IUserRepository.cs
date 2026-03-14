// UMS — University Management System
// Key:     UMS-IDENTITY-P2-001
// Service: Identity
// Layer:   Application / Interfaces
namespace Identity.Application.Interfaces;

using Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;

public interface IUserRepository
{
    // ── Lookups ───────────────────────────────────────────────────────────
    Task<ApplicationUser?> FindByIdAsync(Guid userId, CancellationToken ct = default);
    Task<ApplicationUser?> FindByEmailAsync(Guid tenantId, string email, CancellationToken ct = default);
    Task<bool>             ExistsAsync(Guid tenantId, string email, CancellationToken ct = default);
    Task<int>              CountByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<IList<ApplicationUser>> ListByTenantAsync(Guid tenantId, int page, int pageSize, CancellationToken ct = default);

    // ── Mutations ─────────────────────────────────────────────────────────
    Task<IdentityResult>   CreateAsync(ApplicationUser user, string password);
    Task                   UpdateAsync(ApplicationUser user);

    // ── Password + lockout ────────────────────────────────────────────────
    Task<bool>             CheckPasswordAsync(ApplicationUser user, string password);
    Task<PasswordCheckResult> CheckPasswordWithLockoutAsync(ApplicationUser user, string password, CancellationToken ct = default);

    // ── Roles ─────────────────────────────────────────────────────────────
    Task<IList<string>>    GetRolesAsync(ApplicationUser user, CancellationToken ct = default);
    Task<IdentityResult>   AddToRoleAsync(ApplicationUser user, string role);
    Task<IdentityResult>   RemoveFromRoleAsync(ApplicationUser user, string role);
}
