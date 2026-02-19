using Identity.Application.DTOs;
using MediatR;

namespace Identity.Application.Features.Auth.Commands;

public sealed record LoginCommand(
    string TenantSlug,
    string Email,
    string Password
) : IRequest<LoginResult>;

public sealed record LoginResult(
    bool Succeeded,
    string? AccessToken,
    string? Error
);
