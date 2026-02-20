using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Student.Application.Features.Students.Commands;
using Student.Application.Features.Students.Queries;
using System.Security.Claims;

namespace Student.API.Endpoints;

public static class StudentEndpoints
{
    public static IEndpointRouteBuilder MapStudentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/students", async (
            CreateStudentRequest req,
            HttpContext httpContext,
            ISender sender,
            CancellationToken ct) =>
        {
            var tenantId = GetTenantId(httpContext);
            if (tenantId == Guid.Empty) return Results.Unauthorized();

            var result = await sender.Send(
                new CreateStudentCommand(tenantId, req.UserId,
                    req.FirstName, req.LastName, req.Email), ct);
            return Results.Created($"/api/students/{result.StudentId}", result);
        }).RequireAuthorization();

        app.MapGet("/api/students/{id:guid}", async (
            Guid id,
            HttpContext httpContext,
            ISender sender,
            CancellationToken ct) =>
        {
            var tenantId = GetTenantId(httpContext);
            if (tenantId == Guid.Empty) return Results.Unauthorized();

            var result = await sender.Send(new GetStudentByIdQuery(id, tenantId), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization();

        app.MapPut("/api/students/{id:guid}/admit", async (
            Guid id,
            HttpContext httpContext,
            ISender sender,
            CancellationToken ct) =>
        {
            var tenantId = GetTenantId(httpContext);
            if (tenantId == Guid.Empty) return Results.Unauthorized();

            await sender.Send(new AdmitStudentCommand(id, tenantId), ct);
            return Results.NoContent();
        }).RequireAuthorization();

        return app;
    }

    private static Guid GetTenantId(HttpContext httpContext)
    {
        var claim = httpContext.User.FindFirstValue("tenant_id");
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}

public sealed record CreateStudentRequest(
    Guid UserId,
    string FirstName,
    string LastName,
    string Email);
