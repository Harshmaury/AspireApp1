// src/Services/Identity/Identity.API/Middleware/GlobalExceptionMiddleware.cs
namespace Identity.API.Middleware;

using Identity.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unhandled exception on {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            await HandleAsync(context, ex);
        }
    }

    private static Task HandleAsync(HttpContext ctx, Exception ex)
    {
        var (status, code, message) = ex switch
        {
            // 404
            TenantNotFoundException           e => (404, e.Code, e.Message),

            // 409
            TenantAlreadyExistsException      e => (409, e.Code, e.Message),
            UserAlreadyExistsException        e => (409, e.Code, e.Message),
            DbUpdateConcurrencyException        => (409, "CONCURRENCY_CONFLICT",
                "The resource was modified by another request. Please retry."),

            // 403
            SelfRegistrationDisabledException e => (403, e.Code, e.Message),
            TenantUserLimitExceededException  e => (403, e.Code, e.Message),

            // 400
            InvalidVerificationTokenException e => (400, e.Code, e.Message),
            ExpiredVerificationTokenException e => (400, e.Code, e.Message),
            FluentValidation.ValidationException e => (400, "VALIDATION_FAILED",
                string.Join("; ", e.Errors.Select(f => f.ErrorMessage))),

            // 401
            UnauthorizedAccessException         => (401, "UNAUTHORIZED",
                "Authentication required."),

            // 500
            _                                   => (500, "INTERNAL_ERROR",
                "An unexpected error occurred.")
        };

        ctx.Response.StatusCode  = status;
        ctx.Response.ContentType = "application/json";

        return ctx.Response.WriteAsJsonAsync(new
        {
            code,
            message,
            traceId = ctx.TraceIdentifier
        });
    }
}
