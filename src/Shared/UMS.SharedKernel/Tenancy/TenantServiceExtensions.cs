// UMS — University Management System
// Key:     UMS-SHARED-P1-002
// Service: SharedKernel
// Layer:   Infrastructure / Tenancy
namespace UMS.SharedKernel.Tenancy;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

public static class TenantServiceExtensions
{
    /// <summary>
    /// Registers the scoped <see cref="TenantContext"/> so it is available
    /// as both the concrete mutable type (for the middleware) and the read-only
    /// <see cref="ITenantContext"/> interface (for DbContexts, handlers, etc.).
    ///
    /// Call once from each service's Infrastructure DependencyInjection.cs:
    ///   services.AddTenantContext();
    /// </summary>
    public static IServiceCollection AddTenantContext(this IServiceCollection services)
    {
        // Single scoped instance — resolved as both the interface and concrete type
        services.AddHttpContextAccessor();
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
        return services;
    }

    /// <summary>
    /// Adds <see cref="TenantContextMiddleware"/> to the pipeline.
    /// Must be called AFTER UseAuthentication() and BEFORE UseAuthorization().
    ///
    ///   app.UseAuthentication();
    ///   app.UseTenantContext();   ← here
    ///   app.UseAuthorization();
    /// </summary>
    public static IApplicationBuilder UseTenantContext(this IApplicationBuilder app)
        => app.UseMiddleware<TenantContextMiddleware>();
}
