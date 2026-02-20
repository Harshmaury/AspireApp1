using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Student.Application.Features.Students.Commands;
using Student.Application.Features.Students.Queries;

namespace Student.API.Endpoints;

public static class StudentEndpoints
{
    public static IEndpointRouteBuilder MapStudentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/students", async (
            CreateStudentRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new CreateStudentCommand(req.TenantId, req.UserId,
                    req.FirstName, req.LastName, req.Email), ct);
            return Results.Created($"/api/students/{result.StudentId}", result);
        }).RequireAuthorization();

        app.MapGet("/api/students/{id:guid}", async (
            Guid id,
            Guid tenantId,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetStudentByIdQuery(id, tenantId), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization();

        app.MapPut("/api/students/{id:guid}/admit", async (
            Guid id,
            Guid tenantId,
            ISender sender,
            CancellationToken ct) =>
        {
            await sender.Send(new AdmitStudentCommand(id, tenantId), ct);
            return Results.NoContent();
        }).RequireAuthorization();

        return app;
    }
}

public sealed record CreateStudentRequest(
    Guid TenantId,
    Guid UserId,
    string FirstName,
    string LastName,
    string Email);
