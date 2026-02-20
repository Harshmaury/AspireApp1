using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Identity.API.Middleware;

public sealed class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var tenantIdClaim = context.User.FindFirstValue("tenant_id");
        var tenantSlug = context.User.FindFirstValue("tenant_slug") ?? string.Empty;
        var tenantTier = context.User.FindFirstValue("tenant_tier") ?? string.Empty;

        if (Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            context.Items["TenantId"] = tenantId;
            context.Items["TenantSlug"] = tenantSlug;
            context.Items["TenantTier"] = tenantTier;
        }

        await _next(context);
    }
}
