// UMS — University Management System
// Key:     UMS-IDENTITY-P2-004
// Service: Identity
// Layer:   Application / Features / Users / Commands
namespace Identity.Application.Features.Users.Commands;

using Identity.Application.Interfaces;
using MediatR;

public sealed record UpdateProfileCommand(
    Guid   UserId,
    string FirstName,
    string LastName) : IRequest<UpdateProfileResult>;

public sealed record UpdateProfileResult(bool Succeeded, string? Error);

internal sealed class UpdateProfileCommandHandler
    : IRequestHandler<UpdateProfileCommand, UpdateProfileResult>
{
    private readonly IUserRepository _users;

    public UpdateProfileCommandHandler(IUserRepository users) => _users = users;

    public async Task<UpdateProfileResult> Handle(
        UpdateProfileCommand request, CancellationToken ct)
    {
        var user = await _users.FindByIdAsync(request.UserId, ct);
        if (user is null)
            return new UpdateProfileResult(false, "User not found.");

        user.UpdateProfile(request.FirstName, request.LastName);
        await _users.UpdateAsync(user);

        return new UpdateProfileResult(true, null);
    }
}
