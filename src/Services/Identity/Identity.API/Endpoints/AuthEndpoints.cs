using Identity.Application.Features.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.Http;
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

        // HTTP 410 Gone â€” permanently replaced by POST /connect/token
        group.MapPost("/login", (Delegate)RemovedAsync)
            .WithName("Login")
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

    private static Task<IResult> RemovedAsync(HttpContext httpContext)
    {
        httpContext.Response.Headers.Append(
            "Link", "</connect/token>; rel=\"successor-version\"");
        return Task.FromResult(Results.Json(
            new { error = "This endpoint has been removed. Use POST /connect/token." },
            statusCode: 410));
    }

    public sealed record RegisterRequest(string TenantSlug, string Email,
        string Password, string FirstName, string LastName);
}