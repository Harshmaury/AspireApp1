// UMS — University Management System
// Key:     UMS-IDENTITY-P2-008
// Service: Identity
// Layer:   Application / Features / Auth / Commands
namespace Identity.Application.Features.Auth.Commands;

using MediatR;

public sealed record RevokeTokenCommand(string Token) : IRequest<RevokeTokenResult>;
public sealed record RevokeTokenResult(bool Succeeded, string? Error);

// Handler lives in Identity.Infrastructure — OpenIddict is an infrastructure concern.
// See: Identity.Infrastructure.Handlers.RevokeTokenCommandHandler
