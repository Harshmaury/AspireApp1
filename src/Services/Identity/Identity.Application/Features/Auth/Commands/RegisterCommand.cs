using Identity.Application.DTOs;
using MediatR;

namespace Identity.Application.Features.Auth.Commands;

public sealed record RegisterCommand(
    string TenantSlug,
    string Email,
    string Password,
    string FirstName,
    string LastName
) : IRequest<RegisterResult>;

public sealed record RegisterResult(
    bool Succeeded,
    Guid? UserId,
    IEnumerable<string> Errors
);
