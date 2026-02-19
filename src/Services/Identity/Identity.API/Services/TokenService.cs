using Identity.Application.Interfaces;
using Identity.Domain.Entities;

namespace Identity.API.Services;

internal sealed class TokenService : ITokenService
{
    public Task<string> GenerateTokenAsync(
        ApplicationUser user,
        Tenant tenant,
        CancellationToken ct = default)
    {
        // Phase 1 placeholder — replaced with OpenIddict token in Phase 2
        var token = $"token::{user.Id}::{tenant.Slug}::{DateTime.UtcNow.Ticks}";
        return Task.FromResult(token);
    }
}
