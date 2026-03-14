// UMS — University Management System
// Key:     UMS-IDENTITY-P2-005
// Service: Identity
// Layer:   Application / Features / Users / Commands
namespace Identity.Application.Features.Users.Commands;

using Identity.Domain.Entities;

using Identity.Application.Interfaces;
using MediatR;

public sealed record DeactivateUserCommand(
    Guid ActorId,
    Guid TargetUserId) : IRequest<DeactivateUserResult>;

public sealed record DeactivateUserResult(bool Succeeded, string? Error);

internal sealed class DeactivateUserCommandHandler
    : IRequestHandler<DeactivateUserCommand, DeactivateUserResult>
{
    private readonly IUserRepository _users;
    private readonly IAuditLogger    _audit;

    public DeactivateUserCommandHandler(IUserRepository users, IAuditLogger audit)
    {
        _users = users;
        _audit = audit;
    }

    public async Task<DeactivateUserResult> Handle(
        DeactivateUserCommand request, CancellationToken ct)
    {
        var user = await _users.FindByIdAsync(request.TargetUserId, ct);
        if (user is null)
            return new DeactivateUserResult(false, "User not found.");

        if (!user.IsActive)
            return new DeactivateUserResult(false, "User is already inactive.");

        user.Deactivate();
        await _users.UpdateAsync(user);

        await _audit.LogAsync(AuditLog.Create(
            action:    AuditActions.Deactivated,
            tenantId:  user.TenantId,
            userId:    request.ActorId,
            succeeded: true,
            details:   $"Deactivated user {request.TargetUserId}"), ct);

        return new DeactivateUserResult(true, null);
    }
}
