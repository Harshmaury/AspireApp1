// src/Services/Identity/Identity.Application/Features/Auth/Commands/ResendVerificationCommand.cs
using MediatR;

namespace Identity.Application.Features.Auth.Commands;

public sealed record ResendVerificationCommand(
    string TenantSlug,
    string Email
) : IRequest<ResendVerificationResult>;

public sealed record ResendVerificationResult(
    bool Succeeded,
    string? Error = null
);
