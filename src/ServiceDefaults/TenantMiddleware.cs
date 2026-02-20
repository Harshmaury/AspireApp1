using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace Microsoft.Extensions.DependencyInjection;

public static class TenantMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantContext(this IApplicationBuilder app)
        => app.UseMiddleware<TenantMiddleware>();
}

public sealed class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var tenantIdClaim = context.User.FindFirstValue("tenant_id");
        var tenantSlug = context.User.FindFirstValue("tenant_slug") ?? string.Empty;

        var resolved = Guid.TryParse(tenantIdClaim, out var tenantId);

        var tenantContext = new TenantContext
        {
            TenantId = tenantId,
            TenantSlug = tenantSlug,
            IsResolved = resolved
        };

        context.Items["TenantContext"] = tenantContext;

        await _next(context);
    }
}
