// src/Services/Identity/Identity.Application/Features/Auth/Commands/RegisterCommandHandler.cs
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.Exceptions;
using MediatR;

namespace Identity.Application.Features.Auth.Commands;

internal sealed class RegisterCommandHandler
    : IRequestHandler<RegisterCommand, RegisterResult>
{
    private readonly IUserRepository _users;
    private readonly ITenantRepository _tenants;

    public RegisterCommandHandler(
        IUserRepository users,
        ITenantRepository tenants)
    {
        _users   = users;
        _tenants = tenants;
    }

    public async Task<RegisterResult> Handle(
        RegisterCommand request, CancellationToken ct)
    {
        // 1. Resolve tenant by slug
        var tenant = await _tenants.FindBySlugAsync(request.TenantSlug, ct)
            ?? throw new TenantNotFoundException(Guid.Empty);

        // 2. Enforce self-registration feature flag
        if (!tenant.Features.AllowSelfRegistration)
            throw new SelfRegistrationDisabledException(tenant.Slug);

        // 3. Enforce per-tenant user cap
        var currentCount = await _users.CountByTenantAsync(tenant.Id, ct);
        if (!tenant.CanAddUsers(currentCount))
            throw new TenantUserLimitExceededException(tenant.Id, tenant.MaxUsers);

        // 4. Check duplicate email within this tenant
        var exists = await _users.ExistsAsync(tenant.Id, request.Email, ct);
        if (exists)
            throw new UserAlreadyExistsException(request.Email);

        // 5. Create domain aggregate - raises UserRegisteredEvent internally
        var user = ApplicationUser.Create(
            tenant.Id,
            request.Email,
            request.FirstName,
            request.LastName);

        // 6. Persist via Identity (handles password hashing)
        var result = await _users.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return new RegisterResult(false, null,
                result.Errors.Select(e => e.Description));

        return new RegisterResult(true, user.Id, []);
    }
}
