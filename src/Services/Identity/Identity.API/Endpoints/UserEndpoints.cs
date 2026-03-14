// UMS — University Management System
// Key:     UMS-IDENTITY-P2-009
// Service: Identity
// Layer:   API / Endpoints
namespace Identity.API.Endpoints;

using Identity.Application.Features.Users.Commands;
using Identity.Application.Features.Users.Queries;
using Identity.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("Users")
            .RequireAuthorization();

        // ── Current user ──────────────────────────────────────────────────
        group.MapGet("/me", GetMeAsync)
            .WithName("GetCurrentUser");

        group.MapPut("/me", UpdateMeAsync)
            .WithName("UpdateCurrentUser");

        // ── Admin — user list ─────────────────────────────────────────────
        group.MapGet("/", ListUsersAsync)
            .WithName("ListUsers")
            .RequireAuthorization(p => p.RequireRole("SuperAdmin", "Admin"));

        // ── Admin — user management ───────────────────────────────────────
        group.MapPut("/{userId:guid}/deactivate", DeactivateAsync)
            .WithName("DeactivateUser")
            .RequireAuthorization(p => p.RequireRole("SuperAdmin", "Admin"));

        group.MapPut("/{userId:guid}/roles", AssignRolesAsync)
            .WithName("AssignRoles")
            .RequireAuthorization(p => p.RequireRole("SuperAdmin"));

        return app;
    }

    // ── Handlers ──────────────────────────────────────────────────────────

    private static async Task<IResult> GetMeAsync(
        ClaimsPrincipal principal,
        ISender sender,
        CancellationToken ct)
    {
        var userId = GetUserId(principal);
        if (userId is null) return Results.Unauthorized();

        var result = await sender.Send(new GetCurrentUserQuery(userId.Value), ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> UpdateMeAsync(
        [FromBody] UpdateProfileRequest request,
        ClaimsPrincipal principal,
        ISender sender,
        CancellationToken ct)
    {
        var userId = GetUserId(principal);
        if (userId is null) return Results.Unauthorized();

        var result = await sender.Send(
            new UpdateProfileCommand(userId.Value, request.FirstName, request.LastName), ct);

        return result.Succeeded
            ? Results.Ok(new { Message = "Profile updated." })
            : Results.BadRequest(new { result.Error });
    }

    private static async Task<IResult> ListUsersAsync(
        ClaimsPrincipal principal,
        ISender sender,
        IUserRepository users,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var tenantId = GetTenantId(principal);
        if (tenantId is null) return Results.Unauthorized();

        var list = await users.ListByTenantAsync(
            tenantId.Value,
            Math.Max(1, page),
            Math.Clamp(pageSize, 1, 100),
            ct);

        return Results.Ok(new
        {
            Page     = page,
            PageSize = pageSize,
            Items    = list.Select(u => new
            {
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                u.FullName,
                u.IsActive,
                u.TenantId,
                u.CreatedAt
            })
        });
    }

    private static async Task<IResult> DeactivateAsync(
        Guid userId,
        ClaimsPrincipal principal,
        ISender sender,
        CancellationToken ct)
    {
        var actorId = GetUserId(principal);
        if (actorId is null) return Results.Unauthorized();

        var result = await sender.Send(
            new DeactivateUserCommand(actorId.Value, userId), ct);

        return result.Succeeded
            ? Results.Ok(new { Message = "User deactivated." })
            : Results.BadRequest(new { result.Error });
    }

    private static async Task<IResult> AssignRolesAsync(
        Guid userId,
        [FromBody] AssignRolesRequest request,
        ClaimsPrincipal principal,
        ISender sender,
        CancellationToken ct)
    {
        var actorId = GetUserId(principal);
        if (actorId is null) return Results.Unauthorized();

        var result = await sender.Send(
            new AssignRolesCommand(actorId.Value, userId, request.Roles), ct);

        return result.Succeeded
            ? Results.Ok(new { Message = "Roles updated." })
            : Results.BadRequest(new { result.Error });
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static Guid? GetUserId(ClaimsPrincipal p)
    {
        var sub = p.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)
               ?? p.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    private static Guid? GetTenantId(ClaimsPrincipal p)
    {
        var tid = p.FindFirstValue("tenant_id");
        return Guid.TryParse(tid, out var id) ? id : null;
    }

    // ── Request records ───────────────────────────────────────────────────

    public sealed record UpdateProfileRequest(string FirstName, string LastName);
    public sealed record AssignRolesRequest(IList<string> Roles);
}
