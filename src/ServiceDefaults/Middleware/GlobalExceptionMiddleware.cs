// ============================================================
// AspireApp1.ServiceDefaults — GlobalExceptionMiddleware
// Canonical RFC 7807-compliant error handler for all UMS APIs.
//
// Maps exception types to HTTP status codes:
//   IDomainException (NOT_FOUND codes) → 404
//   IDomainException (CONFLICT codes)  → 409
//   IDomainException (INVALID_ codes)  → 422
//   IDomainException (other)           → 400
//   KeyNotFoundException               → 404
//   UnauthorizedAccessException        → 401
//   ArgumentException                  → 422
//   InvalidOperationException (not found / already exists) → 404 / 409
//   Exception (default)                → 500
// ============================================================
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using UMS.SharedKernel.Exceptions;

namespace AspireApp1.ServiceDefaults.Middleware;

/// <summary>
/// ASP.NET Core middleware that catches all unhandled exceptions and
/// returns a structured JSON response conforming to RFC 7807.
/// </summary>
public sealed class GlobalExceptionMiddleware(
    RequestDelegate                     next,
    ILogger<GlobalExceptionMiddleware>  logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleAsync(ctx, ex);
        }
    }

    private static async Task HandleAsync(HttpContext ctx, Exception ex)
    {
        var (status, code, message) = ex switch
        {
            IDomainException de when DomainExceptionCodes.NotFound.Contains(de.Code)
                => (404, de.Code, ex.Message),

            IDomainException de when DomainExceptionCodes.Conflict.Contains(de.Code)
                => (409, de.Code, ex.Message),

            IDomainException de when de.Code.StartsWith("INVALID_") || de.Code == "INVALID_STATE"
                => (422, de.Code, ex.Message),

            IDomainException de
                => (400, de.Code, ex.Message),

            KeyNotFoundException
                => (404, "NOT_FOUND", ex.Message),

            UnauthorizedAccessException
                => (401, "UNAUTHORIZED", ex.Message),

            ArgumentException                          // domain guard failures (empty name, etc.)
                => (422, "VALIDATION_ERROR", ex.Message),

            InvalidOperationException when ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                => (404, "NOT_FOUND", ex.Message),

            InvalidOperationException when ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase)
                => (409, "CONFLICT", ex.Message),

            InvalidOperationException
                => (400, "INVALID_OPERATION", ex.Message),

            _   => (500, "INTERNAL_ERROR", "An unexpected error occurred.")
        };

        ctx.Response.StatusCode  = status;
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsJsonAsync(new { code, message });
    }
}
