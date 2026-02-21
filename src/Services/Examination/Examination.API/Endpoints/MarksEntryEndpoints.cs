using MediatR;
using Examination.Application.MarksEntry.Commands;
namespace Examination.API.Endpoints;
public static class MarksEntryEndpoints
{
    public static void MapMarksEntryEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/marks").RequireAuthorization();
        group.MapPost("/", async (EnterMarksCommand cmd, IMediator mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(cmd, ct);
            return Results.Created($"/api/marks/{id}", new { id });
        });
        group.MapPut("/{id}/submit", async (Guid id, Guid tenantId, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new SubmitMarksCommand(tenantId, id), ct);
            return Results.NoContent();
        });
        group.MapPut("/{id}/approve", async (Guid id, Guid tenantId, Guid approvedBy, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new ApproveMarksCommand(tenantId, id, approvedBy), ct);
            return Results.NoContent();
        });
        group.MapPut("/{id}/publish", async (Guid id, Guid tenantId, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new PublishMarksCommand(tenantId, id), ct);
            return Results.NoContent();
        });
    }
}
