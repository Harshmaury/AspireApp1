using Hostel.API.Middleware;
using Hostel.Application.Complaints.Commands;
using Hostel.Application.Complaints.Queries;
using Hostel.Domain.Enums;
using MediatR;
namespace Hostel.API.Endpoints;
public static class ComplaintEndpoints
{
    public static void MapComplaintEndpoints(this WebApplication app)
    {
        var grp = app.MapGroup("/api/complaints").RequireAuthorization();

        grp.MapGet("/", async (HttpContext ctx, IMediator med, ComplaintStatus? status, int page = 1, int pageSize = 20) =>
        {
            var tid = HostelTenantContext.GetTenantId(ctx);
            var result = await med.Send(new GetComplaintsQuery(tid, status, page, pageSize));
            return Results.Ok(new { success = true, data = result });
        });

        grp.MapPost("/", async (RaiseComplaintRequest req, HttpContext ctx, IMediator med) =>
        {
            var tid = HostelTenantContext.GetTenantId(ctx);
            var result = await med.Send(new RaiseComplaintCommand(tid, req.StudentId, req.HostelId, req.Category, req.Description));
            return Results.Created($"/api/complaints/{result.Id}", new { success = true, data = result });
        });

        grp.MapPatch("/{id:guid}/status", async (Guid id, UpdateStatusRequest req, HttpContext ctx, IMediator med) =>
        {
            var tid = HostelTenantContext.GetTenantId(ctx);
            await med.Send(new UpdateComplaintStatusCommand(id, tid, req.Action, req.ResolutionNote));
            return Results.Ok(new { success = true });
        });
    }
}
public sealed record RaiseComplaintRequest(Guid StudentId, Guid HostelId, ComplaintCategory Category, string Description);
public sealed record UpdateStatusRequest(string Action, string? ResolutionNote);
