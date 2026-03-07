// src/Services/Identity/Identity.Application/Features/Auth/Commands/VerifyEmailCommand.cs
using MediatR;

namespace Identity.Application.Features.Auth.Commands;

public sealed record VerifyEmailCommand(
    string RawToken
) : IRequest<VerifyEmailResult>;

public sealed record VerifyEmailResult(
    bool Succeeded,
    string? Error = null
);
