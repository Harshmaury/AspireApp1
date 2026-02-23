using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

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
        // ADR-006: Gateway owns auth — read forwarded headers, not JWT claims
        var tenantIdHeader = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        var tenantSlug     = context.Request.Headers["X-Tenant-Slug"].FirstOrDefault() ?? string.Empty;

        var resolved = Guid.TryParse(tenantIdHeader, out var tenantId);

        context.Items["TenantContext"] = new TenantContext
        {
            TenantId   = tenantId,
            TenantSlug = tenantSlug,
            IsResolved = resolved
        };

        await _next(context);
    }
}