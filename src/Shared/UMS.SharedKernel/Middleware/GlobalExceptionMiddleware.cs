using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using UMS.SharedKernel.Exceptions;

namespace UMS.SharedKernel.Middleware;

public sealed class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger)
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
            // Per-service domain exceptions via interface
            IDomainException de when DomainExceptionCodes.NotFound.Contains(de.Code)
                => (404, de.Code, ex.Message),
            IDomainException de when DomainExceptionCodes.Conflict.Contains(de.Code)
                => (409, de.Code, ex.Message),
            IDomainException de when de.Code.StartsWith("INVALID_") || de.Code == "INVALID_STATE"
                => (422, de.Code, ex.Message),
            IDomainException de
                => (400, de.Code, ex.Message),

            // Identity DomainException (no Code property)
            KeyNotFoundException
                => (404, "NOT_FOUND", ex.Message),
            TenantNotFoundHelper
                => (404, "TENANT_NOT_FOUND", ex.Message),
            UnauthorizedAccessException
                => (401, "UNAUTHORIZED", ex.Message),
            InvalidOperationException when ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                => (404, "NOT_FOUND", ex.Message),
            InvalidOperationException when ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase)
                => (409, "CONFLICT", ex.Message),
            InvalidOperationException
                => (400, "INVALID_OPERATION", ex.Message),

            // Bare Exception "not found" (Fee/Exam legacy throws)
            Exception when ex.GetType() == typeof(Exception) && ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                => (404, "NOT_FOUND", ex.Message),

            _   => (500, "INTERNAL_ERROR", "An unexpected error occurred.")
        };

        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsJsonAsync(new { code, message });
    }

    // Helper to avoid referencing Identity.Domain directly
    private sealed class TenantNotFoundHelper : Exception { }
}
