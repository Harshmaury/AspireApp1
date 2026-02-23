using System.Security.Claims;

namespace ApiGateway.Middleware;

public class ClaimsForwardingMiddleware
{
    private readonly RequestDelegate _next;

    public ClaimsForwardingMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx)
    {
        if (ctx.User.Identity?.IsAuthenticated == true)
        {
            var tenantId = ctx.User.FindFirstValue("tenant_id")
                        ?? ctx.User.FindFirstValue("tenantid")
                        ?? string.Empty;
            var userId   = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? ctx.User.FindFirstValue("sub")
                        ?? string.Empty;
            var role     = ctx.User.FindFirstValue(ClaimTypes.Role)
                        ?? ctx.User.FindFirstValue("role")
                        ?? string.Empty;

            if (!string.IsNullOrEmpty(tenantId))
                ctx.Request.Headers["X-Tenant-Id"] = tenantId;
            if (!string.IsNullOrEmpty(userId))
                ctx.Request.Headers["X-User-Id"] = userId;
            if (!string.IsNullOrEmpty(role))
                ctx.Request.Headers["X-User-Role"] = role;
        }

        await _next(ctx);
    }
}
