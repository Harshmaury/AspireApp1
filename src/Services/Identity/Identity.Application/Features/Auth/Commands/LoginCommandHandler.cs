using Identity.Application.Interfaces;
using Identity.Domain.Exceptions;
using MediatR;

namespace Identity.Application.Features.Auth.Commands;

internal sealed class LoginCommandHandler
    : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly IUserRepository _users;
    private readonly ITenantRepository _tenants;
    private readonly ITokenService _tokens;

    public LoginCommandHandler(
        IUserRepository users,
        ITenantRepository tenants,
        ITokenService tokens)
    {
        _users = users;
        _tenants = tenants;
        _tokens = tokens;
    }

    public async Task<LoginResult> Handle(
        LoginCommand request, CancellationToken ct)
    {
        // 1. Resolve tenant
        var tenant = await _tenants.FindBySlugAsync(request.TenantSlug, ct)
            ?? throw new TenantNotFoundException(Guid.Empty);

        // 2. Find user within tenant
        var user = await _users.FindByEmailAsync(tenant.Id, request.Email, ct);
        if (user is null || !user.IsActive)
            return new LoginResult(false, null, "Invalid credentials.");

        // 3. Verify password
        var valid = await _users.CheckPasswordAsync(user, request.Password);
        if (!valid)
            return new LoginResult(false, null, "Invalid credentials.");

        // 4. Record login timestamp
        user.RecordLogin();
        await _users.UpdateAsync(user);

        // 5. Issue token via OpenIddict (delegated to token service in API layer)
        var token = await _tokens.GenerateTokenAsync(user, tenant, ct);

        return new LoginResult(true, token, null);
    }
}
