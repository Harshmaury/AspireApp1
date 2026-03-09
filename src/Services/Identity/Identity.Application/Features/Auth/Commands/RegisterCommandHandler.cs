using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.Exceptions;
using MediatR;

namespace Identity.Application.Features.Auth.Commands;

internal sealed class RegisterCommandHandler
    : IRequestHandler<RegisterCommand, RegisterResult>
{
    private readonly IUserRepository   _users;
    private readonly ITenantRepository _tenants;
    private readonly IAuditLogger      _audit;

    public RegisterCommandHandler(
        IUserRepository   users,
        ITenantRepository tenants,
        IAuditLogger      audit)
    {
        _users   = users;
        _tenants = tenants;
        _audit   = audit;
    }

    public async Task<RegisterResult> Handle(
        RegisterCommand request, CancellationToken ct)
    {
        var tenant = await _tenants.FindBySlugAsync(request.TenantSlug, ct)
            ?? throw new TenantNotFoundException(Guid.Empty);

        if (!tenant.Features.AllowSelfRegistration)
            throw new SelfRegistrationDisabledException(tenant.Slug);

        var currentCount = await _users.CountByTenantAsync(tenant.Id, ct);
        if (!tenant.CanAddUsers(currentCount))
            throw new TenantUserLimitExceededException(tenant.Id, tenant.MaxUsers);

        var exists = await _users.ExistsAsync(tenant.Id, request.Email, ct);
        if (exists)
            throw new UserAlreadyExistsException(request.Email);

        var user = ApplicationUser.Create(
            tenant.Id,
            request.Email,
            request.FirstName,
            request.LastName);

        var result = await _users.CreateAsync(user, request.Password);

        await _audit.LogAsync(AuditLog.Create(
            action:    AuditActions.Register,
            tenantId:  tenant.Id,
            userId:    result.Succeeded ? user.Id : null,
            succeeded: result.Succeeded,
            details:   result.Succeeded ? null : string.Join("; ", result.Errors.Select(e => e.Description))),
            ct);

        if (!result.Succeeded)
            return new RegisterResult(false, null, result.Errors.Select(e => e.Description));

        return new RegisterResult(true, user.Id, []);
    }
}
