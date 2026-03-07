
using System.Security.Claims;

namespace Identity.API.Middleware;

public sealed class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, UMS.SharedKernel.Tenancy.TenantContext tenantContext)
    {
        var tenantIdClaim = context.User.FindFirstValue("tenant_id");
        var slug = context.User.FindFirstValue("tenant_slug") ?? string.Empty;
        var tier = context.User.FindFirstValue("tenant_tier") ?? string.Empty;

        if (Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            tenantContext.SetTenant(tenantId, slug, tier);

            // Keep HttpContext.Items in sync for any legacy code
            context.Items["TenantId"]   = tenantId;
            context.Items["TenantSlug"] = slug;
            context.Items["TenantTier"] = tier;
        }

        await _next(context);
    }
}
