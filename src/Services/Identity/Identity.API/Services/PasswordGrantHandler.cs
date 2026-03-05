using Identity.Application.Features.Auth.Commands;
using MediatR;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Identity.API.Services;

public sealed class PasswordGrantHandler
    : IOpenIddictServerHandler<OpenIddictServerEvents.HandleTokenRequestContext>
{
    private readonly ISender _mediator;

    public static OpenIddictServerHandlerDescriptor Descriptor =>
        OpenIddictServerHandlerDescriptor
            .CreateBuilder<OpenIddictServerEvents.HandleTokenRequestContext>()
            .UseScopedHandler<PasswordGrantHandler>()
            .SetOrder(500)
            .Build();

    public PasswordGrantHandler(ISender mediator) => _mediator = mediator;

    public async ValueTask HandleAsync(OpenIddictServerEvents.HandleTokenRequestContext context)
    {
        if (!context.Request.IsPasswordGrantType())
            return;

        var username = context.Request.Username ?? string.Empty;
        string tenantSlug, email;

        if (username.Contains('|'))
        {
            var parts = username.Split('|', 2);
            tenantSlug = parts[0];
            email      = parts[1];
        }
        else
        {
            tenantSlug = context.Request["tenant_slug"]?.ToString() ?? string.Empty;
            email      = username;
        }

        var result = await _mediator.Send(
            new ValidateCredentialsCommand(tenantSlug, email,
                context.Request.Password ?? string.Empty));

        if (!result.Succeeded)
        {
            context.Reject(error: Errors.InvalidGrant, description: result.Error);
            return;
        }

        var user   = result.User!;
        var tenant = result.Tenant!;
        var roles  = result.Roles ?? [];

        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: Claims.Name,
            roleType: Claims.Role);

        identity.AddClaim(Claims.Subject,            user.Id.ToString());
        identity.AddClaim(Claims.Name,               user.Email!);
        identity.AddClaim(Claims.Email,              user.Email!);
        identity.AddClaim(Claims.GivenName,          user.FirstName);
        identity.AddClaim(Claims.FamilyName,         user.LastName);
        identity.AddClaim("tenant_id",               user.TenantId.ToString());
        identity.AddClaim("tenant_slug",             tenant.Slug);
        identity.AddClaim("tenant_tier",             tenant.Tier.ToString());
        identity.AddClaim("subscription_status",     tenant.SubscriptionStatus.ToString());

        foreach (var role in roles)
            identity.AddClaim(Claims.Role, role);

        foreach (var claim in identity.Claims)
            claim.SetDestinations(Destinations.AccessToken, Destinations.IdentityToken);

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(context.Request.GetScopes());

        context.SignIn(principal);
    }
}