using MediatR;
using Student.Application.Features.Students.Commands;
using Student.Application.Features.Students.Queries;

namespace Student.API.Endpoints;

public static class StudentEndpoints
{
    public static IEndpointRouteBuilder MapStudentEndpoints(this IEndpointRouteBuilder app)
    {
        // CREATE
        app.MapPost("/api/students", async (CreateStudentRequest req, HttpContext ctx, ISender s, CancellationToken ct) =>
        {
            var tid = TenantId(ctx); if (tid == Guid.Empty) return Results.Unauthorized();
            var r = await s.Send(new CreateStudentCommand(tid, req.UserId, req.FirstName, req.LastName, req.Email), ct);
            return Results.Created($"/api/students/{r.StudentId}", r);
        }).RequireAuthorization();

        // GET BY ID
        app.MapGet("/api/students/{id:guid}", async (Guid id, HttpContext ctx, ISender s, CancellationToken ct) =>
        {
            var tid = TenantId(ctx); if (tid == Guid.Empty) return Results.Unauthorized();
            var r = await s.Send(new GetStudentByIdQuery(id, tid), ct);
            return r is null ? Results.NotFound() : Results.Ok(r);
        }).RequireAuthorization();

        // GET ALL — ?status=Enrolled&page=1&pageSize=20
        app.MapGet("/api/students", async (
            HttpContext ctx, ISender s, CancellationToken ct,
            string? status = null, int page = 1, int pageSize = 20) =>
        {
            var tid = TenantId(ctx); if (tid == Guid.Empty) return Results.Unauthorized();
            var r = await s.Send(new GetAllStudentsQuery(tid, status, page, pageSize), ct);
            return Results.Ok(r);
        }).RequireAuthorization();

        // UPDATE DETAILS
        app.MapPut("/api/students/{id:guid}", async (Guid id, UpdateStudentRequest req, HttpContext ctx, ISender s, CancellationToken ct) =>
        {
            var tid = TenantId(ctx); if (tid == Guid.Empty) return Results.Unauthorized();
            await s.Send(new UpdateStudentCommand(id, tid, req.FirstName, req.LastName, req.Email), ct);
            return Results.NoContent();
        }).RequireAuthorization();

        // LIFECYCLE
        app.MapPut("/api/students/{id:guid}/admit", async (Guid id, HttpContext ctx, ISender s, CancellationToken ct) =>
        {
            var tid = TenantId(ctx); if (tid == Guid.Empty) return Results.Unauthorized();
            await s.Send(new AdmitStudentCommand(id, tid), ct);
            return Results.NoContent();
        }).RequireAuthorization();

        app.MapPut("/api/students/{id:guid}/enroll", async (Guid id, HttpContext ctx, ISender s, CancellationToken ct) =>
        {
            var tid = TenantId(ctx); if (tid == Guid.Empty) return Results.Unauthorized();
            await s.Send(new EnrollStudentCommand(id, tid), ct);
            return Results.NoContent();
        }).RequireAuthorization();

        app.MapPut("/api/students/{id:guid}/suspend", async (Guid id, SuspendRequest req, HttpContext ctx, ISender s, CancellationToken ct) =>
        {
            var tid = TenantId(ctx); if (tid == Guid.Empty) return Results.Unauthorized();
            await s.Send(new SuspendStudentCommand(id, tid, req.Reason), ct);
            return Results.NoContent();
        }).RequireAuthorization();

        app.MapPut("/api/students/{id:guid}/reinstate", async (Guid id, HttpContext ctx, ISender s, CancellationToken ct) =>
        {
            var tid = TenantId(ctx); if (tid == Guid.Empty) return Results.Unauthorized();
            await s.Send(new ReinstateStudentCommand(id, tid), ct);
            return Results.NoContent();
        }).RequireAuthorization();

        app.MapPut("/api/students/{id:guid}/graduate", async (Guid id, HttpContext ctx, ISender s, CancellationToken ct) =>
        {
            var tid = TenantId(ctx); if (tid == Guid.Empty) return Results.Unauthorized();
            await s.Send(new GraduateStudentCommand(id, tid), ct);
            return Results.NoContent();
        }).RequireAuthorization();

        app.MapPut("/api/students/{id:guid}/archive", async (Guid id, HttpContext ctx, ISender s, CancellationToken ct) =>
        {
            var tid = TenantId(ctx); if (tid == Guid.Empty) return Results.Unauthorized();
            await s.Send(new ArchiveStudentCommand(id, tid), ct);
            return Results.NoContent();
        }).RequireAuthorization();

        return app;
    }

    private static Guid TenantId(HttpContext ctx)
    {
        var h = ctx.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        return Guid.TryParse(h, out var id) ? id : Guid.Empty;
    }
}

public sealed record CreateStudentRequest(Guid UserId, string FirstName, string LastName, string Email);
public sealed record UpdateStudentRequest(string FirstName, string LastName, string Email);
public sealed record SuspendRequest(string Reason);
