// src/Services/Identity/Identity.API/Program.cs
using Identity.API.Endpoints;
using Identity.API.Middleware;
using Identity.API.Services;
using Identity.Application;
using Identity.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using System.Security.Cryptography;
using System.Threading.RateLimiting;
using UMS.SharedKernel.Extensions;
using static OpenIddict.Abstractions.OpenIddictConstants;

var builder = WebApplication.CreateBuilder(args);

// -- Aspire + defaults -------------------------------------------------------
builder.AddServiceDefaults();
builder.AddSerilogDefaults();
builder.AddNpgsqlHealthCheck("IdentityDb");

// -- Application layers ------------------------------------------------------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// -- Auth + API --------------------------------------------------------------
builder.Services.AddAuthorization();
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHostedService<OutboxRelayService>();

// -- Rate Limiting (GAP-001) -------------------------------------------------
builder.Services.AddRateLimiter(opts =>
{
    // /connect/token - 10 attempts per minute per IP (brute force protection)
    opts.AddSlidingWindowLimiter("token_endpoint", o =>
    {
        o.PermitLimit          = 10;
        o.Window               = TimeSpan.FromMinutes(1);
        o.SegmentsPerWindow    = 6;
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit           = 0;
    });

    // /api/auth/register - 5 registrations per 10 minutes per IP
    opts.AddFixedWindowLimiter("register_endpoint", o =>
    {
        o.PermitLimit = 5;
        o.Window      = TimeSpan.FromMinutes(10);
    });

    opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opts.OnRejected = async (ctx, ct) =>
    {
        ctx.HttpContext.Response.ContentType = "application/json";
        await ctx.HttpContext.Response.WriteAsJsonAsync(new
        {
            code    = "RATE_LIMITED",
            message = "Too many requests. Please slow down.",
            retryAfterSeconds = ctx.Lease.TryGetMetadata(
                MetadataName.RetryAfter, out var ra) ? (int)ra.TotalSeconds : 60
        }, ct);
    };
});

// -- OpenIddict --------------------------------------------------------------
var isDev = builder.Environment.IsDevelopment();

var clientSecret = builder.Configuration["OpenIddict:ClientSecret"]
    ?? (isDev ? "api-gateway-secret-dev" : null);

if (clientSecret is null)
    throw new InvalidOperationException(
        "OpenIddict:ClientSecret is not configured. " +
        "Set it via Kubernetes secret: kubectl create secret generic identity-secrets ...");

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
        "OpenIddict:SigningKeyXml is required in production.");

if (!isDev && encryptionKey is null)
    throw new InvalidOperationException(
        "OpenIddict:EncryptionKeyXml is required in production.");

builder.Services.AddOpenIddict()
    .AddServer(options =>
    {
        options.SetTokenEndpointUris("/connect/token");
        options.SetRevocationEndpointUris("/connect/revoke");

        options.AllowPasswordFlow()
               .AllowRefreshTokenFlow();

        // Token lifetimes - short access token, sliding refresh
        options.SetAccessTokenLifetime(isDev
            ? TimeSpan.FromHours(1)
            : TimeSpan.FromMinutes(15));
        options.SetRefreshTokenLifetime(TimeSpan.FromDays(14));
        options.SetRefreshTokenReuseLeeway(TimeSpan.FromSeconds(30));

        if (isDev)
        {
            options.AddEphemeralEncryptionKey();
            options.AddEphemeralSigningKey();
        }
        else
        {
            // Reference tokens stored in DB - revocable
            options.UseReferenceRefreshTokens();
            options.UseReferenceAccessTokens();

            options.AddSigningKey(signingKey!);
            options.AddEncryptionKey(encryptionKey!);
        }

        options.DisableAccessTokenEncryption();
        options.RegisterScopes(
            Scopes.Email, Scopes.Profile, Scopes.Roles,
            Scopes.OfflineAccess, Scopes.OpenId, "api");

        options.AddEventHandler(PasswordGrantHandler.Descriptor);

        var aspNet = options.UseAspNetCore();
        if (isDev) aspNet.DisableTransportSecurityRequirement();

        aspNet.EnableTokenEndpointPassthrough();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

// -- Build app ---------------------------------------------------------------
var app = builder.Build();

// -- Middleware order matters - do NOT rearrange -----------------------------

// 1. Global exception handler - must be first to catch everything
app.UseGlobalExceptionHandler();

// 2. Serilog request logging
app.UseSerilogDefaults();

// 3. Security headers (production hardening)
if (!isDev)
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"]        = "DENY";
    ctx.Response.Headers["Referrer-Policy"]        = "strict-origin-when-cross-origin";
    ctx.Response.Headers["X-Correlation-Id"]       =
        ctx.Request.Headers["X-Correlation-Id"].FirstOrDefault()
        ?? Guid.NewGuid().ToString("N");
    await next();
});

// 4. Rate limiting
app.UseRateLimiter();

// 5. Routing
app.UseRouting();

// 6. Auth
app.UseAuthentication();

// 7. Tenant context resolution (after auth so JWT claims are available)
app.UseMiddleware<Identity.API.Middleware.TenantMiddleware>();

// 8. Authorization
app.UseAuthorization();

// -- Seed + OpenIddict client registration -----------------------------------
await IdentitySeeder.SeedAsync(app.Services);

using (var scope = app.Services.CreateScope())
{
    var manager = scope.ServiceProvider
        .GetRequiredService<IOpenIddictApplicationManager>();

    var descriptor = new OpenIddictApplicationDescriptor
    {
        ClientId     = "api-gateway",
        ClientSecret = clientSecret,
        DisplayName  = "API Gateway",
        Permissions  =
        {
            Permissions.Endpoints.Token,
            Permissions.Endpoints.Revocation,
            Permissions.GrantTypes.Password,
            Permissions.GrantTypes.RefreshToken,
            Permissions.Scopes.Email,
            Permissions.Scopes.Profile,
            Permissions.Scopes.Roles,
            Permissions.Prefixes.Scope + "offline_access",
            Permissions.Prefixes.Scope + "openid",
            Permissions.Prefixes.Scope + "api"
        }
    };

    var existing = await manager.FindByClientIdAsync("api-gateway");
    if (existing is null) await manager.CreateAsync(descriptor);
    else await manager.UpdateAsync(existing, descriptor);
}

// -- Endpoints ---------------------------------------------------------------
if (isDev) app.MapOpenApi();

app.MapDefaultEndpoints();

app.MapAuthEndpoints();
app.MapTenantEndpoints();
app.MapRegionHealthEndpoints();

app.Run();
