using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Identity.Application.Features.Auth.Commands;

public sealed record ValidateCredentialsCommand(
    string TenantSlug,
    string Email,
    string Password
) : IRequest<ValidateCredentialsResult>;

public sealed record ValidateCredentialsResult(
    bool Succeeded,
    ApplicationUser? User,
    Tenant? Tenant,
    IList<string>? Roles,
    string? Error
);

internal sealed class ValidateCredentialsCommandHandler
    : IRequestHandler<ValidateCredentialsCommand, ValidateCredentialsResult>
{
    private readonly IUserRepository _users;
    private readonly ITenantRepository _tenants;
    private readonly bool _writeAllowed;

    public ValidateCredentialsCommandHandler(
        IUserRepository users,
        ITenantRepository tenants,
        IConfiguration configuration)
    {
        _users        = users;
        _tenants      = tenants;
        _writeAllowed = bool.TryParse(
            configuration["REGION_WRITE_ALLOWED"], out var b) && b;
    }

    public async Task<ValidateCredentialsResult> Handle(
        ValidateCredentialsCommand request, CancellationToken ct)
    {
        // 1. Tenant lookup
        var tenant = await _tenants.FindBySlugAsync(request.TenantSlug, ct);
        if (tenant is null)
            return Fail("Tenant not found.");

        // 2. Explicit subscription guard
        if (tenant.SubscriptionStatus == SubscriptionStatus.Suspended ||
            tenant.SubscriptionStatus == SubscriptionStatus.Cancelled)
            return Fail("Tenant account is not available.");

        // 3. User lookup — scoped to tenant
        var user = await _users.FindByEmailAsync(tenant.Id, request.Email, ct);
        if (user is null)
            return Fail("Invalid credentials.");

        // 4. Explicit user active guard
        if (!user.IsActive)
            return Fail("Invalid credentials.");

        // 5. Lockout-aware password check
        var passwordResult = await _users.CheckPasswordWithLockoutAsync(
            user, request.Password, ct);

        if (passwordResult == PasswordCheckResult.LockedOut)
            return Fail("Account is temporarily locked. Try again later.");

        if (passwordResult == PasswordCheckResult.Failed)
            return Fail("Invalid credentials.");

        // 6. Fetch roles for token claims
        var roles = await _users.GetRolesAsync(user, ct);

        // 7. Record login only on PRIMARY region
        if (_writeAllowed)
        {
            user.RecordLogin();
            await _users.UpdateAsync(user);
        }

        return new ValidateCredentialsResult(true, user, tenant, roles, null);
    }

    private static ValidateCredentialsResult Fail(string error) =>
        new(false, null, null, null, error);
}