using Hostel.API.Middleware;
using Hostel.Application.Rooms.Commands;
using Hostel.Application.Rooms.Queries;
using Hostel.Domain.Enums;
using MediatR;
namespace Hostel.API.Endpoints;
public static class RoomEndpoints
{
    public static void MapRoomEndpoints(this WebApplication app)
    {
        var grp = app.MapGroup("/api/rooms");

        grp.MapGet("/", async (HttpContext ctx, IMediator med, Guid hostelId, int page = 1, int pageSize = 20) =>
        {
            var tid = HostelTenantContext.GetTenantId(ctx);
            var result = await med.Send(new GetRoomsQuery(hostelId, tid, page, pageSize));
            return Results.Ok(new { success = true, data = result });
        });

        grp.MapPost("/", async (CreateRoomRequest req, HttpContext ctx, IMediator med) =>
        {
            var tid = HostelTenantContext.GetTenantId(ctx);
            var result = await med.Send(new CreateRoomCommand(tid, req.HostelId, req.RoomNumber, req.Floor, req.Type, req.Capacity));
            return Results.Created($"/api/rooms/{result.Id}", new { success = true, data = result });
        });

        grp.MapPatch("/{id:guid}/maintenance", async (Guid id, SetMaintenanceRequest req, HttpContext ctx, IMediator med) =>
        {
            var tid = HostelTenantContext.GetTenantId(ctx);
            await med.Send(new SetMaintenanceCommand(id, tid, req.Maintenance));
            return Results.Ok(new { success = true });
        });
    }
}
public sealed record CreateRoomRequest(Guid HostelId, string RoomNumber, int Floor, RoomType Type, int Capacity);
public sealed record SetMaintenanceRequest(bool Maintenance);
