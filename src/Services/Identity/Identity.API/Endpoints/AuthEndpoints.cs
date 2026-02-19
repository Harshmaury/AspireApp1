using Identity.Application.Features.Auth.Commands;
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
            .WithSummary("Register a new user within a tenant");

        group.MapPost("/login", LoginAsync)
            .WithName("Login")
            .WithSummary("Login and receive an access token");

        return app;
    }

    private static async Task<IResult> RegisterAsync(
        [FromBody] RegisterRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new RegisterCommand(
            request.TenantSlug,
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName);

        var result = await sender.Send(command, ct);

        return result.Succeeded
            ? Results.Ok(new { result.UserId, Message = "Registration successful." })
            : Results.BadRequest(new { Errors = result.Errors });
    }

    private static async Task<IResult> LoginAsync(
        [FromBody] LoginRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new LoginCommand(
            request.TenantSlug,
            request.Email,
            request.Password);

        var result = await sender.Send(command, ct);

        return result.Succeeded
            ? Results.Ok(new { result.AccessToken })
            : Results.Unauthorized();
    }

    public sealed record RegisterRequest(
        string TenantSlug,
        string Email,
        string Password,
        string FirstName,
        string LastName);

    public sealed record LoginRequest(
        string TenantSlug,
        string Email,
        string Password);
}
