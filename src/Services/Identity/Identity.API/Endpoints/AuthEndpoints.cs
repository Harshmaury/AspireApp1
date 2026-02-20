using Identity.Application.Features.Auth.Commands;
using Identity.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication");

        group.MapPost("/register", RegisterAsync)
            .WithName("Register")
            .AllowAnonymous();

        group.MapPost("/login", LoginAsync)
            .WithName("Login")
            .AllowAnonymous();

        group.MapPost("/debug-login", DebugLoginAsync)
            .WithName("DebugLogin")
            .AllowAnonymous();

        return app;
    }

    private static async Task<IResult> RegisterAsync(
        [FromBody] RegisterRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new RegisterCommand(
            request.TenantSlug, request.Email, request.Password,
            request.FirstName, request.LastName), ct);

        return result.Succeeded
            ? Results.Ok(new { result.UserId, Message = "Registration successful." })
            : Results.BadRequest(new { Errors = result.Errors });
    }

    private static async Task<IResult> LoginAsync(
        [FromBody] LoginRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new LoginCommand(request.TenantSlug, request.Email, request.Password), ct);

        return result.Succeeded
            ? Results.Ok(new
            {
                accessToken = result.AccessToken,
                tokenType = "Bearer",
                expiresIn = 3600
            })
            : Results.Unauthorized();
    }

    private static async Task<IResult> DebugLoginAsync(
        [FromBody] LoginRequest request,
        IUserRepository users,
        ITenantRepository tenants,
        CancellationToken ct)
    {
        var tenant = await tenants.FindBySlugAsync(request.TenantSlug, ct);
        if (tenant is null)
            return Results.Ok(new { step = "tenant_lookup", result = "FAILED" });

        var user = await users.FindByEmailAsync(tenant.Id, request.Email, ct);
        if (user is null)
            return Results.Ok(new { step = "user_lookup", result = "FAILED", tenantId = tenant.Id });

        var valid = await users.CheckPasswordAsync(user, request.Password);
        return Results.Ok(new { step = "password_check", result = valid ? "PASSED" : "FAILED" });
    }

    public sealed record RegisterRequest(string TenantSlug, string Email,
        string Password, string FirstName, string LastName);
    public sealed record LoginRequest(string TenantSlug, string Email, string Password);
}
