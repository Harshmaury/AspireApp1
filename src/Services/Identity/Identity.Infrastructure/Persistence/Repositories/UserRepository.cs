using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository : IUserRepository
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserRepository(UserManager<ApplicationUser> userManager)
        => _userManager = userManager;

    public async Task<ApplicationUser?> FindByEmailAsync(Guid tenantId, string email, CancellationToken ct = default)
        => await _userManager.Users
            .Where(u => u.TenantId == tenantId && u.NormalizedEmail == email.ToUpperInvariant())
            .FirstOrDefaultAsync(ct);

    public async Task<bool> ExistsAsync(Guid tenantId, string email, CancellationToken ct = default)
        => await _userManager.Users
            .AnyAsync(u => u.TenantId == tenantId && u.NormalizedEmail == email.ToUpperInvariant(), ct);

    public async Task<IdentityResult> CreateAsync(ApplicationUser user, string password)
        => await _userManager.CreateAsync(user, password);

    public async Task<bool> CheckPasswordAsync(ApplicationUser user, string password)
        => await _userManager.CheckPasswordAsync(user, password);

    public async Task UpdateAsync(ApplicationUser user)
        => await _userManager.UpdateAsync(user);
}
