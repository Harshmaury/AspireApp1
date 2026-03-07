// src/Services/Identity/Identity.Application/Features/Auth/Commands/ForgotPasswordCommand.cs
using MediatR;

namespace Identity.Application.Features.Auth.Commands;

public sealed record ForgotPasswordCommand(
    string TenantSlug,
    string Email
) : IRequest<ForgotPasswordResult>;

public sealed record ForgotPasswordResult(
    bool Succeeded
);
