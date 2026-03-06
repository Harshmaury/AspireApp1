using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Academic.API.Middleware;

public static class TenantConstants
{
    public const string TenantIdKey = "TenantId";
}

public sealed class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        var tenantClaim =
            ctx.User.FindFirst("tenant_id") ??
            ctx.User.FindFirst("tenantId");

        if (tenantClaim == null)
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await ctx.Response.WriteAsync("TenantId missing from token.");
            return;
        }

        if (!Guid.TryParse(tenantClaim.Value, out var tenantId))
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await ctx.Response.WriteAsync("Invalid TenantId format.");
            return;
        }

        ctx.Items[TenantConstants.TenantIdKey] = tenantId;

        await _next(ctx);
    }
}

public static class HttpContextExtensions
{
    public static Guid GetTenantId(this HttpContext ctx)
    {
        if (ctx.Items.TryGetValue(TenantConstants.TenantIdKey, out var val) && val is Guid tid)
            return tid;

        throw new UnauthorizedAccessException("TenantId missing.");
    }
}