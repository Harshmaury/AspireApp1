namespace Attendance.API.Middleware;
public sealed class TenantMiddleware
{
    private readonly RequestDelegate _next;
    public TenantMiddleware(RequestDelegate next) => _next = next;
    public async Task InvokeAsync(HttpContext context)
    {
        var tenantIdClaim = context.User.FindFirst("tenant_id")?.Value;
        if (!string.IsNullOrWhiteSpace(tenantIdClaim) && Guid.TryParse(tenantIdClaim, out var tenantId))
            context.Items["TenantId"] = tenantId;
        await _next(context);
    }
}
