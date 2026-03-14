// UMS — University Management System
// Key:     UMS-IDENTITY-P2-003
// Service: Identity
// Layer:   Application / Features / Users / Queries
namespace Identity.Application.Features.Users.Queries;

using Identity.Application.Interfaces;
using MediatR;

public sealed record GetCurrentUserQuery(Guid UserId) : IRequest<CurrentUserResult?>;

public sealed record CurrentUserResult(
    Guid   UserId,
    Guid   TenantId,
    string Email,
    string FirstName,
    string LastName,
    string FullName,
    bool   IsActive,
    IList<string> Roles);

internal sealed class GetCurrentUserQueryHandler
    : IRequestHandler<GetCurrentUserQuery, CurrentUserResult?>
{
    private readonly IUserRepository _users;

    public GetCurrentUserQueryHandler(IUserRepository users) => _users = users;

    public async Task<CurrentUserResult?> Handle(
        GetCurrentUserQuery request, CancellationToken ct)
    {
        var user = await _users.FindByIdAsync(request.UserId, ct);
        if (user is null) return null;

        var roles = await _users.GetRolesAsync(user, ct);

        return new CurrentUserResult(
            UserId:    user.Id,
            TenantId:  user.TenantId,
            Email:     user.Email!,
            FirstName: user.FirstName,
            LastName:  user.LastName,
            FullName:  user.FullName,
            IsActive:  user.IsActive,
            Roles:     roles);
    }
}
