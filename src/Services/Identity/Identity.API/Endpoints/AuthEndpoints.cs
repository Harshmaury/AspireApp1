// src/Services/Identity/Identity.API/Endpoints/AuthEndpoints.cs
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

        // ── Registration ────────────────────────────────────────────────────
        group.MapPost("/register", RegisterAsync)
            .WithName("Register")
            .AllowAnonymous()
            .RequireRateLimiting("register_endpoint");

        // ── Login removed - use POST /connect/token ─────────────────────────
        group.MapPost("/login", (Delegate)RemovedAsync)
            .WithName("Login")
            .AllowAnonymous();

        // ── Email verification ───────────────────────────────────────────────
        group.MapPost("/verify-email", VerifyEmailAsync)
            .WithName("VerifyEmail")
            .AllowAnonymous();

        group.MapPost("/resend-verification", ResendVerificationAsync)
            .WithName("ResendVerification")
            .AllowAnonymous();

        // ── Password reset ───────────────────────────────────────────────────
        group.MapPost("/forgot-password", ForgotPasswordAsync)
            .WithName("ForgotPassword")
            .AllowAnonymous();

        group.MapPost("/reset-password", ResetPasswordAsync)
            .WithName("ResetPassword")
            .AllowAnonymous();

        return app;
    }

    // ── Handlers ─────────────────────────────────────────────────────────────

    private static async Task<IResult> RegisterAsync(
        [FromBody] RegisterRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new RegisterCommand(
            request.TenantSlug,
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName), ct);

        return result.Succeeded
            ? Results.Ok(new { result.UserId, Message = "Registration successful." })
            : Results.BadRequest(new { Errors = result.Errors });
    }

    private static async Task<IResult> VerifyEmailAsync(
        [FromBody] VerifyEmailRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new VerifyEmailCommand(request.Token), ct);

        return result.Succeeded
            ? Results.Ok(new { Message = "Email verified successfully." })
            : Results.BadRequest(new { result.Error });
    }

    private static async Task<IResult> ResendVerificationAsync(
        [FromBody] ResendVerificationRequest request,
        ISender sender,
        CancellationToken ct)
    {
        // Always return 200 — prevents email enumeration
        await sender.Send(new ResendVerificationCommand(
            request.TenantSlug,
            request.Email), ct);

        return Results.Ok(new
        {
            Message = "If that email exists and is unverified, a new verification link has been sent."
        });
    }

    private static async Task<IResult> ForgotPasswordAsync(
        [FromBody] ForgotPasswordRequest request,
        ISender sender,
        CancellationToken ct)
    {
        // Always return 200 — prevents email enumeration
        await sender.Send(new ForgotPasswordCommand(
            request.TenantSlug,
            request.Email), ct);

        return Results.Ok(new
        {
            Message = "If that email exists, a password reset link has been sent."
        });
    }

    private static async Task<IResult> ResetPasswordAsync(
        [FromBody] ResetPasswordRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new ResetPasswordCommand(
            request.Token,
            request.NewPassword), ct);

        return result.Succeeded
            ? Results.Ok(new { Message = "Password reset successfully." })
            : Results.BadRequest(new { result.Error });
    }

    private static Task<IResult> RemovedAsync(HttpContext httpContext)
    {
        httpContext.Response.Headers.Append(
            "Link", "</connect/token>; rel=\"successor-version\"");
        return Task.FromResult(Results.Json(
            new { error = "This endpoint has been removed. Use POST /connect/token." },
            statusCode: 410));
    }

    // ── Request records ───────────────────────────────────────────────────────

    public sealed record RegisterRequest(
        string TenantSlug,
        string Email,
        string Password,
        string FirstName,
        string LastName);

    public sealed record VerifyEmailRequest(
        string Token);

    public sealed record ResendVerificationRequest(
        string TenantSlug,
        string Email);

    public sealed record ForgotPasswordRequest(
        string TenantSlug,
        string Email);

    public sealed record ResetPasswordRequest(
        string Token,
        string NewPassword);
}

