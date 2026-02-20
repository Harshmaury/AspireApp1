using Identity.Application.Interfaces;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Identity.API.Services;

public sealed class PasswordGrantHandler : IOpenIddictServerHandler<OpenIddictServerEvents.HandleTokenRequestContext>
{
    private readonly IUserRepository _users;
    private readonly ITenantRepository _tenants;

    public PasswordGrantHandler(IUserRepository users, ITenantRepository tenants)
    {
        _users = users;
        _tenants = tenants;
    }

    public async ValueTask HandleAsync(OpenIddictServerEvents.HandleTokenRequestContext context)
    {
        if (!context.Request.IsPasswordGrantType())
            return;

        var username = context.Request.Username ?? string.Empty;
        var password = context.Request.Password ?? string.Empty;

        string tenantSlug;
        string email;

        if (username.Contains('|'))
        {
            var parts = username.Split('|', 2);
            tenantSlug = parts[0];
            email = parts[1];
        }
        else
        {
            tenantSlug = context.Request["tenant_slug"]?.ToString() ?? "test-uni";
            email = username;
        }

        var tenant = await _tenants.FindBySlugAsync(tenantSlug);
        if (tenant is null)
        {
            context.Reject(error: Errors.InvalidGrant, description: "Tenant not found.");
            return;
        }

        var user = await _users.FindByEmailAsync(tenant.Id, email);
        if (user is null || !user.IsActive)
        {
            context.Reject(error: Errors.InvalidGrant, description: "Invalid credentials.");
            return;
        }

        var valid = await _users.CheckPasswordAsync(user, password);
        if (!valid)
        {
            context.Reject(error: Errors.InvalidGrant, description: "Invalid credentials.");
            return;
        }

        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: Claims.Name,
            roleType: Claims.Role);

        identity.AddClaim(Claims.Subject, user.Id.ToString());
        identity.AddClaim(Claims.Name, user.Email!);
        identity.AddClaim(Claims.Email, user.Email!);
        identity.AddClaim(Claims.GivenName, user.FirstName);
        identity.AddClaim(Claims.FamilyName, user.LastName);
        identity.AddClaim("tenant_id", user.TenantId.ToString());
        identity.AddClaim("tenant_slug", tenant.Slug);

        foreach (var claim in identity.Claims)
            claim.SetDestinations(Destinations.AccessToken);

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(context.Request.GetScopes());

        context.SignIn(principal);
    }
}
