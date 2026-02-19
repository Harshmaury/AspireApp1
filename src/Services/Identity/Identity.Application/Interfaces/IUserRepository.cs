using Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Identity.Application.Interfaces;

public interface IUserRepository
{
    Task<ApplicationUser?> FindByEmailAsync(Guid tenantId, string email, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid tenantId, string email, CancellationToken ct = default);
    Task<IdentityResult> CreateAsync(ApplicationUser user, string password);
    Task<bool> CheckPasswordAsync(ApplicationUser user, string password);
    Task UpdateAsync(ApplicationUser user);
}
