// UMS — University Management System
// Key:     UMS-IDENTITY-P2-011
// Service: Identity
// Layer:   Infrastructure / Persistence / Repositories
namespace Identity.Infrastructure.Persistence.Repositories;

using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

internal sealed class UserRepository : IUserRepository
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext         _db;

    public UserRepository(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db)
    {
        _userManager = userManager;
        _db          = db;
    }

    public async Task<ApplicationUser?> FindByIdAsync(
        Guid userId, CancellationToken ct = default)
        => await _userManager.FindByIdAsync(userId.ToString());

    public async Task<ApplicationUser?> FindByEmailAsync(
        Guid tenantId, string email, CancellationToken ct = default)
        => await _db.Users
            .Where(u => u.TenantId == tenantId &&
                        u.NormalizedEmail == email.ToUpperInvariant())
            .FirstOrDefaultAsync(ct);

    public async Task<bool> ExistsAsync(
        Guid tenantId, string email, CancellationToken ct = default)
        => await _db.Users
            .AnyAsync(u => u.TenantId == tenantId &&
                           u.NormalizedEmail == email.ToUpperInvariant(), ct);

    public async Task<int> CountByTenantAsync(
        Guid tenantId, CancellationToken ct = default)
        => await _db.Users.CountAsync(u => u.TenantId == tenantId, ct);

    public async Task<IList<ApplicationUser>> ListByTenantAsync(
        Guid tenantId, int page, int pageSize, CancellationToken ct = default)
        => await _db.Users
            .Where(u => u.TenantId == tenantId)
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public async Task<IdentityResult> CreateAsync(
        ApplicationUser user, string password)
        => await _userManager.CreateAsync(user, password);

    public async Task UpdateAsync(ApplicationUser user)
        => await _userManager.UpdateAsync(user);

    public async Task<bool> CheckPasswordAsync(
        ApplicationUser user, string password)
        => await _userManager.CheckPasswordAsync(user, password);

    public async Task<PasswordCheckResult> CheckPasswordWithLockoutAsync(
        ApplicationUser user, string password, CancellationToken ct = default)
    {
        if (await _userManager.IsLockedOutAsync(user))
            return PasswordCheckResult.LockedOut;

        var valid = await _userManager.CheckPasswordAsync(user, password);

        if (!valid)
        {
            await _userManager.AccessFailedAsync(user);
            return PasswordCheckResult.Failed;
        }

        await _userManager.ResetAccessFailedCountAsync(user);
        return PasswordCheckResult.Success;
    }

    public async Task<IList<string>> GetRolesAsync(
        ApplicationUser user, CancellationToken ct = default)
        => await _userManager.GetRolesAsync(user);

    public async Task<IdentityResult> AddToRoleAsync(
        ApplicationUser user, string role)
        => await _userManager.AddToRoleAsync(user, role);

    public async Task<IdentityResult> RemoveFromRoleAsync(
        ApplicationUser user, string role)
        => await _userManager.RemoveFromRoleAsync(user, role);
}
