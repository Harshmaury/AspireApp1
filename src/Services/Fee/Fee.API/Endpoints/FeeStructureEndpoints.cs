using MediatR;
using Fee.Application.FeeStructure.Commands;
namespace Fee.API.Endpoints;
public static class FeeStructureEndpoints
{
    public static void MapFeeStructureEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/fee-structures").RequireAuthorization();
        group.MapPost("/", async (CreateFeeStructureCommand cmd, IMediator mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(cmd, ct);
            return Results.Created($"/api/fee-structures/{id}", new { id });
        });
    }
}
