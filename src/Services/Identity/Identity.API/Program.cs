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
using static OpenIddict.Abstractions.OpenIddictConstants;
using Identity.Application.Features.Auth.Commands;

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
        // Required — even if we do not use the passthrough endpoint
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
            "api");
        options.UseAspNetCore()
               .EnableTokenEndpointPassthrough()
               .DisableTransportSecurityRequirement();
        options.AddEventHandler<OpenIddict.Server.OpenIddictServerEvents.HandleTokenRequestContext>(b => b.UseScopedHandler<PasswordGrantHandler>());
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

    var manager = scope.ServiceProvider
        .GetRequiredService<IOpenIddictApplicationManager>();

    var existing = await manager.FindByClientIdAsync("api-gateway");
    if (existing is null)
    {
        await manager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = "api-gateway",
            ClientSecret = "api-gateway-secret",
            DisplayName = "API Gateway",
            Permissions =
            {
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.Password,
                Permissions.GrantTypes.RefreshToken,
                Permissions.Scopes.Email,
                Permissions.Scopes.Profile,
                Permissions.Scopes.Roles,
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

app.Run();


























