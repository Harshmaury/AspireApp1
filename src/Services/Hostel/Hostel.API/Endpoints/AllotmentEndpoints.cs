using Hostel.API.Middleware;
using Hostel.Application.Allotments.Commands;
using Hostel.Application.Allotments.Queries;
using MediatR;
namespace Hostel.API.Endpoints;
public static class AllotmentEndpoints
{
    public static void MapAllotmentEndpoints(this WebApplication app)
    {
        var grp = app.MapGroup("/api/allotments");

        grp.MapGet("/", async (HttpContext ctx, IMediator med, int page = 1, int pageSize = 20) =>
        {
            var tid = HostelTenantContext.GetTenantId(ctx);
            var result = await med.Send(new GetAllotmentsQuery(tid, page, pageSize));
            return Results.Ok(new { success = true, data = result });
        });

        grp.MapGet("/student/{studentId:guid}", async (Guid studentId, string academicYear, HttpContext ctx, IMediator med) =>
        {
            var tid = HostelTenantContext.GetTenantId(ctx);
            var result = await med.Send(new GetStudentAllotmentQuery(studentId, academicYear, tid));
            return result is null ? Results.NotFound(new { success = false, error = new { code = "ALLOTMENT_NOT_FOUND" } })
                : Results.Ok(new { success = true, data = result });
        });

        grp.MapPost("/", async (AllocateRoomRequest req, HttpContext ctx, IMediator med) =>
        {
            var tid = HostelTenantContext.GetTenantId(ctx);
            var result = await med.Send(new AllocateRoomCommand(tid, req.StudentId, req.RoomId, req.HostelId, req.AcademicYear, req.BedNumber));
            return Results.Created($"/api/allotments/{result.Id}", new { success = true, data = result });
        });

        grp.MapPatch("/{id:guid}/vacate", async (Guid id, HttpContext ctx, IMediator med) =>
        {
            var tid = HostelTenantContext.GetTenantId(ctx);
            await med.Send(new VacateRoomCommand(id, tid));
            return Results.Ok(new { success = true });
        });
    }
}
public sealed record AllocateRoomRequest(Guid StudentId, Guid RoomId, Guid HostelId, string AcademicYear, int BedNumber);
