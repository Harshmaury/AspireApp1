namespace Academic.API.Middleware;
public sealed class TenantMiddleware
{
    private readonly RequestDelegate _next;
    public TenantMiddleware(RequestDelegate next) => _next = next;
    public async Task InvokeAsync(HttpContext ctx)
    {
        var tenantClaim = ctx.User.FindFirst("tenant_id") ?? ctx.User.FindFirst("tenantId");
        if (tenantClaim is not null && Guid.TryParse(tenantClaim.Value, out var tenantId))
            ctx.Items["TenantId"] = tenantId;
        await _next(ctx);
    }
}
public static class HttpContextExtensions
{
    public static Guid GetTenantId(this HttpContext ctx)
    {
        if (ctx.Items.TryGetValue("TenantId", out var val) && val is Guid tid) return tid;
        throw new UnauthorizedAccessException("TenantId not found in token.");
    }
}