namespace Hostel.API.Middleware;
public static class HostelTenantContext
{
    public static Guid GetTenantId(HttpContext ctx)
    {
        if (ctx.Items.TryGetValue("TenantId", out var val) && val is Guid id) return id;
        throw new UnauthorizedAccessException("Tenant context not found.");
    }
}
