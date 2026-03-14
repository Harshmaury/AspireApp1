// UMS — University Management System
// Key:     UMS-IDENTITY-P2-006
// Service: Identity
// Layer:   Application / Features / Users / Commands
namespace Identity.Application.Features.Users.Commands;

using Identity.Domain.Entities;

using Identity.Application.Interfaces;
using MediatR;

public sealed record AssignRolesCommand(
    Guid          ActorId,
    Guid          TargetUserId,
    IList<string> Roles) : IRequest<AssignRolesResult>;

public sealed record AssignRolesResult(bool Succeeded, string? Error);

internal sealed class AssignRolesCommandHandler
    : IRequestHandler<AssignRolesCommand, AssignRolesResult>
{
    private static readonly HashSet<string> AllowedRoles =
        ["SuperAdmin", "Admin", "Faculty", "Student"];

    private readonly IUserRepository _users;
    private readonly IAuditLogger    _audit;

    public AssignRolesCommandHandler(IUserRepository users, IAuditLogger audit)
    {
        _users = users;
        _audit = audit;
    }

    public async Task<AssignRolesResult> Handle(
        AssignRolesCommand request, CancellationToken ct)
    {
        var invalid = request.Roles.Where(r => !AllowedRoles.Contains(r)).ToList();
        if (invalid.Count > 0)
            return new AssignRolesResult(false, $"Invalid roles: {string.Join(", ", invalid)}");

        var user = await _users.FindByIdAsync(request.TargetUserId, ct);
        if (user is null)
            return new AssignRolesResult(false, "User not found.");

        var current = await _users.GetRolesAsync(user, ct);

        // Remove roles not in new set
        foreach (var role in current.Except(request.Roles))
            await _users.RemoveFromRoleAsync(user, role);

        // Add new roles
        foreach (var role in request.Roles.Except(current))
            await _users.AddToRoleAsync(user, role);

        await _audit.LogAsync(AuditLog.Create(
            action:    AuditActions.RolesAssigned,
            tenantId:  user.TenantId,
            userId:    request.ActorId,
            succeeded: true,
            details:   $"Roles set to [{string.Join(", ", request.Roles)}] for user {request.TargetUserId}"), ct);

        return new AssignRolesResult(true, null);
    }
}
