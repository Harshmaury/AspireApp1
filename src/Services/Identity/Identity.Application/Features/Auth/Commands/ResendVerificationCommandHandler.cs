// UMS — University Management System
// Key:     UMS-IDENTITY-P2-016
// Service: Identity
// Layer:   Application / Features / Auth / Commands
// src/Services/Identity/Identity.Application/Features/Auth/Commands/ResendVerificationCommandHandler.cs
namespace Identity.Application.Features.Auth.Commands;

using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.Events;
using MediatR;
using Microsoft.Extensions.Configuration;

internal sealed class ResendVerificationCommandHandler
    : IRequestHandler<ResendVerificationCommand, ResendVerificationResult>
{
    private readonly IUserRepository              _users;
    private readonly ITenantRepository            _tenants;
    private readonly IVerificationTokenRepository _tokens;
    private readonly string                       _baseUrl;

    public ResendVerificationCommandHandler(
        IUserRepository              users,
        ITenantRepository            tenants,
        IVerificationTokenRepository tokens,
        IConfiguration               configuration)
    {
        _users   = users;
        _tenants = tenants;
        _tokens  = tokens;
        _baseUrl = configuration["App:BaseUrl"] ?? "https://app.ums.edu";
    }

    public async Task<ResendVerificationResult> Handle(
        ResendVerificationCommand request, CancellationToken ct)
    {
        // Always return success — prevents email enumeration
        var tenant = await _tenants.FindBySlugAsync(request.TenantSlug, ct);
        if (tenant is null) return new ResendVerificationResult(true);

        var user = await _users.FindByEmailAsync(tenant.Id, request.Email, ct);
        if (user is null || user.EmailConfirmed) return new ResendVerificationResult(true);

        // Invalidate any existing verification tokens
        await _tokens.InvalidateByUserAsync(
            user.Id, TokenPurpose.EmailVerification, ct);

        // Create new token
        var (token, rawToken) = VerificationToken.Create(
            user.Id, tenant.Id, TokenPurpose.EmailVerification);

        await _tokens.CreateAsync(token, ct);

        // Raise domain event — rawToken + expiry carried in event; Notification service builds URL
        user.RaiseEmailVerificationRequested(
            rawToken:  rawToken,
            expiresAt: token.ExpiresAt);

        await _users.UpdateAsync(user);

        return new ResendVerificationResult(true);
    }
}
