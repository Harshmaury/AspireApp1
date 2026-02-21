using Hostel.API.Middleware;
using Hostel.Application.Hostels.Commands;
using Hostel.Application.Hostels.Queries;
using Hostel.Domain.Enums;
using MediatR;
namespace Hostel.API.Endpoints;
public static class HostelEndpoints
{
    public static void MapHostelEndpoints(this WebApplication app)
    {
        var grp = app.MapGroup("/api/hostels").RequireAuthorization();

        grp.MapGet("/", async (HttpContext ctx, IMediator med, int page = 1, int pageSize = 20) =>
        {
            var tid = HostelTenantContext.GetTenantId(ctx);
            var result = await med.Send(new GetHostelsQuery(tid, page, pageSize));
            return Results.Ok(new { success = true, data = result });
        });

        grp.MapGet("/{id:guid}", async (Guid id, HttpContext ctx, IMediator med) =>
        {
            var tid = HostelTenantContext.GetTenantId(ctx);
            var result = await med.Send(new GetHostelByIdQuery(id, tid));
            return result is null ? Results.NotFound(new { success = false, error = new { code = "HOSTEL_NOT_FOUND" } })
                : Results.Ok(new { success = true, data = result });
        });

        grp.MapPost("/", async (CreateHostelRequest req, HttpContext ctx, IMediator med) =>
        {
            var tid = HostelTenantContext.GetTenantId(ctx);
            var result = await med.Send(new CreateHostelCommand(tid, req.Name, req.Type, req.TotalRooms, req.WardenName, req.WardenContact));
            return Results.Created($"/api/hostels/{result.Id}", new { success = true, data = result });
        });

        grp.MapPatch("/{id:guid}/warden", async (Guid id, UpdateWardenRequest req, HttpContext ctx, IMediator med) =>
        {
            var tid = HostelTenantContext.GetTenantId(ctx);
            await med.Send(new UpdateWardenCommand(id, tid, req.WardenName, req.WardenContact));
            return Results.Ok(new { success = true });
        });
    }
}
public sealed record CreateHostelRequest(string Name, HostelType Type, int TotalRooms, string WardenName, string WardenContact);
public sealed record UpdateWardenRequest(string WardenName, string WardenContact);
