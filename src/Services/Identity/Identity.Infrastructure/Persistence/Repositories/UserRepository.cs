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

    // Legacy — used by existing callers that do not need lockout tracking
    public async Task<bool> CheckPasswordAsync(ApplicationUser user, string password)
        => await _userManager.CheckPasswordAsync(user, password);

    // Lockout-aware password check — use this for all login flows
    // Mirrors the logic inside SignInManager.PasswordSignInAsync without
    // touching cookies or authentication schemes
    public async Task<PasswordCheckResult> CheckPasswordWithLockoutAsync(
        ApplicationUser user, string password, CancellationToken ct = default)
    {
        // 1. Reject immediately if already locked out
        if (await _userManager.IsLockedOutAsync(user))
            return PasswordCheckResult.LockedOut;

        // 2. Verify password
        var correct = await _userManager.CheckPasswordAsync(user, password);

        if (correct)
        {
            // 3. Reset fail counter on success
            await _userManager.ResetAccessFailedCountAsync(user);
            return PasswordCheckResult.Success;
        }

        // 4. Increment fail counter — may trigger lockout
        await _userManager.AccessFailedAsync(user);

        // 5. Check if this failure triggered lockout
        if (await _userManager.IsLockedOutAsync(user))
            return PasswordCheckResult.LockedOut;

        return PasswordCheckResult.Failed;
    }

    public async Task<IList<string>> GetRolesAsync(ApplicationUser user, CancellationToken ct = default)
        => await _userManager.GetRolesAsync(user);

    public async Task UpdateAsync(ApplicationUser user)
        => await _userManager.UpdateAsync(user);
}