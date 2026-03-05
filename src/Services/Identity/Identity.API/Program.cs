using Identity.API.Endpoints;
using Identity.API.Services;
using Identity.Application;
using Identity.Infrastructure;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using System.Security.Cryptography;
using UMS.SharedKernel.Extensions;
using static OpenIddict.Abstractions.OpenIddictConstants;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddSerilogDefaults();
builder.AddNpgsqlHealthCheck("IdentityDb");
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();
builder.Services.AddHostedService<OutboxRelayService>();

var isDev = builder.Environment.IsDevelopment();

var clientSecret = builder.Configuration["OpenIddict:ClientSecret"]
    ?? (isDev ? "api-gateway-secret-dev" : null);

if (clientSecret is null)
    throw new InvalidOperationException(
        "OpenIddict:ClientSecret is not configured. Set it via Kubernetes secret.");

// FIX I1: Ephemeral keys were used in all environments. Every pod restart
// generated new keys, invalidating all active tokens across the fleet.
// Production now loads persistent RSA keys from config (Kubernetes secret).
// Store base64-encoded RSA XML in:
//   OpenIddict:SigningKeyXml      — RSA 2048-bit XML private key
//   OpenIddict:EncryptionKeyXml  — RSA 2048-bit XML private key
// To generate: var rsa = RSA.Create(2048); Console.WriteLine(Convert.ToBase64String(Encoding.UTF8.GetBytes(rsa.ToXmlString(true))));
RsaSecurityKey? LoadRsaKey(string configKey)
{
    var b64 = builder.Configuration[configKey];
    if (string.IsNullOrWhiteSpace(b64)) return null;
    var xml = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(b64));
    var rsa = RSA.Create();
    rsa.FromXmlString(xml);
    return new RsaSecurityKey(rsa);
}

var signingKey    = isDev ? null : LoadRsaKey("OpenIddict:SigningKeyXml");
var encryptionKey = isDev ? null : LoadRsaKey("OpenIddict:EncryptionKeyXml");

if (!isDev && signingKey is null)
    throw new InvalidOperationException(
        "OpenIddict:SigningKeyXml is not configured. Set it via Kubernetes secret. " +
        "Generate with: var rsa = RSA.Create(2048); Convert.ToBase64String(Encoding.UTF8.GetBytes(rsa.ToXmlString(true)))");

if (!isDev && encryptionKey is null)
    throw new InvalidOperationException(
        "OpenIddict:EncryptionKeyXml is not configured. Set it via Kubernetes secret.");

builder.Services.AddOpenIddict()
    .AddServer(options =>
    {
        options.SetTokenEndpointUris("/connect/token");
        options.AllowPasswordFlow()
               .AllowRefreshTokenFlow();

        if (isDev)
        {
            // Ephemeral keys are acceptable in development — no shared state between restarts
            options.AddEphemeralEncryptionKey();
            options.AddEphemeralSigningKey();
        }
        else
        {
            // Production: persistent RSA keys loaded from Kubernetes secrets
            options.AddSigningKey(signingKey!);
            options.AddEncryptionKey(encryptionKey!);
        }

        options.DisableAccessTokenEncryption();
        options.RegisterScopes(
            Scopes.Email,
            Scopes.Profile,
            Scopes.Roles,
            Scopes.OfflineAccess,
            Scopes.OpenId,
            "api");

        options.AddEventHandler(PasswordGrantHandler.Descriptor);

        var aspNetCoreBuilder = options.UseAspNetCore();
        if (isDev)
            aspNetCoreBuilder.DisableTransportSecurityRequirement();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

var app = builder.Build();
app.UseSerilogDefaults();
app.UseGlobalExceptionHandler();

await IdentitySeeder.SeedAsync(app.Services);

using (var scope = app.Services.CreateScope())
{
    var manager  = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
    var existing = await manager.FindByClientIdAsync("api-gateway");
    if (existing is null)
    {
        await manager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId     = "api-gateway",
            ClientSecret = clientSecret,
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

app.MapDefaultEndpoints();
app.MapAuthEndpoints();
app.MapTenantEndpoints();
app.MapRegionHealthEndpoints();
app.Run();
