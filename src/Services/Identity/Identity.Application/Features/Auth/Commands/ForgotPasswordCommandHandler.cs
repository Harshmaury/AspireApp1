// UMS — University Management System
// Key:     UMS-IDENTITY-P2-015
// Service: Identity
// Layer:   Application / Features / Auth / Commands
// src/Services/Identity/Identity.Application/Features/Auth/Commands/ForgotPasswordCommandHandler.cs
namespace Identity.Application.Features.Auth.Commands;

using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.Events;
using MediatR;
using Microsoft.Extensions.Configuration;

internal sealed class ForgotPasswordCommandHandler
    : IRequestHandler<ForgotPasswordCommand, ForgotPasswordResult>
{
    private readonly IUserRepository              _users;
    private readonly ITenantRepository            _tenants;
    private readonly IVerificationTokenRepository _tokens;
    private readonly IAuditLogger                 _audit;
    private readonly string                       _baseUrl;

    public ForgotPasswordCommandHandler(
        IUserRepository              users,
        ITenantRepository            tenants,
        IVerificationTokenRepository tokens,
        IAuditLogger                 audit,
        IConfiguration               configuration)
    {
        _users   = users;
        _tenants = tenants;
        _tokens  = tokens;
        _audit   = audit;
        _baseUrl = configuration["App:BaseUrl"] ?? "https://app.ums.edu";
    }

    public async Task<ForgotPasswordResult> Handle(
        ForgotPasswordCommand request, CancellationToken ct)
    {
        // Always return success — prevents email enumeration
        var tenant = await _tenants.FindBySlugAsync(request.TenantSlug, ct);
        if (tenant is null) return new ForgotPasswordResult(true);

        var user = await _users.FindByEmailAsync(tenant.Id, request.Email, ct);
        if (user is null) return new ForgotPasswordResult(true);

        if (!user.IsActive) return new ForgotPasswordResult(true);

        // Invalidate any existing active reset tokens for this user
        await _tokens.InvalidateByUserAsync(
            user.Id, TokenPurpose.PasswordReset, ct);

        // Create new token
        var (token, rawToken) = VerificationToken.Create(
            user.Id, tenant.Id, TokenPurpose.PasswordReset);

        await _tokens.CreateAsync(token, ct);

        // Raise domain event — rawToken + expiry carried in event; Notification service builds URL
        user.RaisePasswordResetRequested(
            rawToken:  rawToken,
            expiresAt: token.ExpiresAt,
            ipAddress: "unknown");

        await _users.UpdateAsync(user);

        await _audit.LogAsync(AuditLog.Create(
            action:    AuditActions.PasswordReset,
            tenantId:  tenant.Id,
            userId:    user.Id,
            succeeded: true,
            details:   "Password reset token issued"), ct);

        return new ForgotPasswordResult(true);
    }
}
