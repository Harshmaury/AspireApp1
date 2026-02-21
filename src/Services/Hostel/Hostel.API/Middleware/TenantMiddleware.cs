namespace Hostel.API.Middleware;
public sealed class TenantMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        var tenantClaim = ctx.User.FindFirst("tenant_id")?.Value;
        if (!string.IsNullOrWhiteSpace(tenantClaim) && Guid.TryParse(tenantClaim, out var tenantId))
            ctx.Items["TenantId"] = tenantId;
        await next(ctx);
    }
}
public static class TenantMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantMiddleware(this IApplicationBuilder app) =>
        app.UseMiddleware<TenantMiddleware>();
}
