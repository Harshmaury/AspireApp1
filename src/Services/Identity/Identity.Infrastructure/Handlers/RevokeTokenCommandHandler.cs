// UMS — University Management System
// Key:     UMS-IDENTITY-P2-008-INFRA
// Service: Identity
// Layer:   Infrastructure / Handlers
namespace Identity.Infrastructure.Handlers;

using Identity.Application.Features.Auth.Commands;
using MediatR;
using OpenIddict.Abstractions;

internal sealed class RevokeTokenCommandHandler
    : IRequestHandler<RevokeTokenCommand, RevokeTokenResult>
{
    private readonly IOpenIddictTokenManager _tokens;

    public RevokeTokenCommandHandler(IOpenIddictTokenManager tokens)
        => _tokens = tokens;

    public async Task<RevokeTokenResult> Handle(
        RevokeTokenCommand request, CancellationToken ct)
    {
        var token = await _tokens.FindByReferenceIdAsync(request.Token, ct)
                 ?? await _tokens.FindByIdAsync(request.Token, ct);

        if (token is null)
            return new RevokeTokenResult(false, "Token not found.");

        await _tokens.TryRevokeAsync(token, ct);
        return new RevokeTokenResult(true, null);
    }
}
