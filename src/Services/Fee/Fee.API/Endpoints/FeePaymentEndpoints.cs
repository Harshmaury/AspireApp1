using MediatR;
using Fee.Application.FeePayment.Commands;
namespace Fee.API.Endpoints;
public static class FeePaymentEndpoints
{
    public static void MapFeePaymentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/fee-payments").RequireAuthorization();
        group.MapPost("/", async (CreateFeePaymentCommand cmd, IMediator mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(cmd, ct);
            return Results.Created($"/api/fee-payments/{id}", new { id });
        });
        group.MapPut("/{id}/success", async (Guid id, Guid tenantId, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new MarkPaymentSuccessCommand(tenantId, id), ct);
            return Results.NoContent();
        });
        group.MapPut("/{id}/failed", async (Guid id, Guid tenantId, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new MarkPaymentFailedCommand(tenantId, id), ct);
            return Results.NoContent();
        });
        group.MapPut("/{id}/refund", async (Guid id, Guid tenantId, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new RefundPaymentCommand(tenantId, id), ct);
            return Results.NoContent();
        });
    }
}
