// src/Services/Identity/Identity.Application/Features/Auth/Commands/ResetPasswordCommand.cs
using MediatR;

namespace Identity.Application.Features.Auth.Commands;

public sealed record ResetPasswordCommand(
    string RawToken,
    string NewPassword
) : IRequest<ResetPasswordResult>;

public sealed record ResetPasswordResult(
    bool Succeeded,
    string? Error = null
);
