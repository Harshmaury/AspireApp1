using UMS.SharedKernel.Extensions;
using Identity.API.Services;
using Identity.Application;
using Identity.Application.Interfaces;
using Identity.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Identity.Infrastructure.Persistence;
using Identity.API.Endpoints;
using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;
using Identity.Application.Features.Auth.Commands;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddSerilogDefaults();
builder.AddNpgsqlHealthCheck("IdentityDb");
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddAuthorization();
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();
builder.Services.AddHostedService<Identity.API.Services.OutboxRelayService>();
builder.Services.AddOpenIddict()
    .AddServer(options =>
    {
        options.SetTokenEndpointUris("/connect/token");
        options.AllowPasswordFlow()
               .AllowRefreshTokenFlow();
        options.AddEphemeralEncryptionKey()
               .AddEphemeralSigningKey();
        options.DisableAccessTokenEncryption();
        options.RegisterScopes(
            Scopes.Email,
            Scopes.Profile,
            Scopes.Roles,
            Scopes.OfflineAccess,
            Scopes.OpenId,
            "api");
        options.UseAspNetCore()
               .EnableTokenEndpointPassthrough()
               .DisableTransportSecurityRequirement();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

var app = builder.Build();
app.UseSerilogDefaults();
app.UseGlobalExceptionHandler();
await Identity.API.Services.IdentitySeeder.SeedAsync(app.Services);

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
    if (!await db.Tenants.AnyAsync(t => t.Slug == "test-uni"))
    {
        db.Tenants.Add(Tenant.Create("Test University", "test-uni"));
        await db.SaveChangesAsync();
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE \"Tenants\" SET \"Id\" = {0} WHERE \"Slug\" = 'test-uni'",
            Guid.Parse("00000000-0000-0000-0000-000000000001"));
    }
    var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
    var existing = await manager.FindByClientIdAsync("api-gateway");
    if (existing is null)
    {
        await manager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId     = "api-gateway",
            ClientSecret = "api-gateway-secret",
            DisplayName  = "API Gateway",
            Permissions  =
            {
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.Password,
                Permissions.GrantTypes.RefreshToken,
                Permissions.Scopes.Email,
                Permissions.Scopes.Profile,
                Permissions.Scopes.Roles,
                Permissions.Prefixes.Scope + "offline_access",
                Permissions.Prefixes.Scope + "openid",
                Permissions.Prefixes.Scope + "api"
            }
        });
    }
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseAuthentication();
app.UseMiddleware<Identity.API.Middleware.TenantMiddleware>();
app.UseAuthorization();

// -- OpenIddict passthrough token endpoint -------------------------------------
app.MapPost("/connect/token", async (
    HttpContext httpContext,
    IUserRepository users,
    ITenantRepository tenants) =>
{
    var request = httpContext.Features.Get<OpenIddictServerAspNetCoreFeature>()?.Transaction?.Request
        ?? throw new InvalidOperationException("OpenIddict server request cannot be retrieved.");

    ClaimsPrincipal principal;

    if (request.IsPasswordGrantType())
    {
        var username = request.Username ?? string.Empty;
        string tenantSlug, email;

        if (username.Contains('|'))
        {
            var parts = username.Split('|', 2);
            tenantSlug = parts[0];
            email      = parts[1];
        }
        else
        {
            tenantSlug = request["tenant_slug"]?.ToString() ?? "test-uni";
            email      = username;
        }

        var tenant = await tenants.FindBySlugAsync(tenantSlug);
        if (tenant is null)
            return Results.Forbid(
                new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error]            = Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Tenant not found."
                }),
                new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme });

        var user = await users.FindByEmailAsync(tenant.Id, email);
        if (user is null || !user.IsActive)
            return Results.Forbid(
                new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error]            = Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Invalid credentials."
                }),
                new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme });

        var valid = await users.CheckPasswordAsync(user, request.Password ?? string.Empty);
        if (!valid)
            return Results.Forbid(
                new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error]            = Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Invalid credentials."
                }),
                new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme });

        var identity = new ClaimsIdentity(
            authenticationType: "OpenIddict",
            nameType: Claims.Name,
            roleType: Claims.Role);

        identity.AddClaim(Claims.Subject,    user.Id.ToString());
        identity.AddClaim(Claims.Name,       user.Email!);
        identity.AddClaim(Claims.Email,      user.Email!);
        identity.AddClaim(Claims.GivenName,  user.FirstName);
        identity.AddClaim(Claims.FamilyName, user.LastName);
        identity.AddClaim("tenant_id",       user.TenantId.ToString());
        identity.AddClaim("tenant_slug",     tenant.Slug);

        foreach (var claim in identity.Claims)
            claim.SetDestinations(Destinations.AccessToken, Destinations.IdentityToken);

        principal = new ClaimsPrincipal(identity);
        principal.SetScopes(request.GetScopes());
    }
    else if (request.IsRefreshTokenGrantType())
    {
        var result = await httpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        if (result.Principal is null)
            return Results.Forbid(
                new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error]            = Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The refresh token is invalid."
                }),
                new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme });
        principal = result.Principal;
    }
    else
    {
        return Results.Forbid(
            new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error]            = Errors.UnsupportedGrantType,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The grant type is not supported."
            }),
            new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme });
    }

    return Results.SignIn(principal, new AuthenticationProperties(), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
}).AllowAnonymous();

app.MapDefaultEndpoints();
app.MapAuthEndpoints();
app.MapTenantEndpoints();
app.Run();
