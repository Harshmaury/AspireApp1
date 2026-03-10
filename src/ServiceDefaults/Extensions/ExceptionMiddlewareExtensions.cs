// ============================================================
// AspireApp1.ServiceDefaults — ExceptionMiddlewareExtensions
// IApplicationBuilder extension to register GlobalExceptionMiddleware.
// Usage in Program.cs:  app.UseGlobalExceptionHandler();
// ============================================================
using Microsoft.AspNetCore.Builder;
using AspireApp1.ServiceDefaults.Middleware;

namespace AspireApp1.ServiceDefaults.Extensions;

/// <summary>
/// Extension methods to register the GlobalExceptionMiddleware.
/// </summary>
public static class ExceptionMiddlewareExtensions
{
    /// <summary>
    /// Registers <see cref="GlobalExceptionMiddleware"/> into the ASP.NET Core pipeline.
    /// Place this BEFORE UseRouting() and MapControllers() for full coverage.
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        => app.UseMiddleware<GlobalExceptionMiddleware>();
}
