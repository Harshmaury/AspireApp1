namespace Academic.API.Middleware;

public sealed class TenantMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        var tenantClaim = ctx.User.FindFirst("tenant_id") ?? ctx.User.FindFirst("tenantId");
        if (tenantClaim is not null && Guid.TryParse(tenantClaim.Value, out var tenantId))
            ctx.Items["TenantId"] = tenantId;
        await next(ctx);
    }
}
public static class HttpContextExtensions
{
    extension(HttpContext ctx)
    {
        public Guid GetTenantId()
        {
            if (ctx.Items.TryGetValue("TenantId", out var val) && val is Guid tid) return tid;
            throw new UnauthorizedAccessException("TenantId not found in token.");
        }
    }
}