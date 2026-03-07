// src/Services/Identity/Identity.Application/Interfaces/IUserRepository.cs
using Identity.Domain.Entities;
using Identity.Application.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Identity.Application.Interfaces;

public interface IUserRepository
{
    Task<ApplicationUser?> FindByEmailAsync(Guid tenantId, string email, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid tenantId, string email, CancellationToken ct = default);
    Task<int> CountByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<IdentityResult> CreateAsync(ApplicationUser user, string password);
    Task<bool> CheckPasswordAsync(ApplicationUser user, string password);
    Task<PasswordCheckResult> CheckPasswordWithLockoutAsync(ApplicationUser user, string password, CancellationToken ct = default);
    Task<IList<string>> GetRolesAsync(ApplicationUser user, CancellationToken ct = default);
    Task UpdateAsync(ApplicationUser user);
}
