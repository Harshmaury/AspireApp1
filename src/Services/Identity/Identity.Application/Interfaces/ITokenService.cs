using Identity.Domain.Entities;

namespace Identity.Application.Interfaces;

public interface ITokenService
{
    Task<string> GenerateTokenAsync(
        ApplicationUser user,
        Tenant tenant,
        CancellationToken ct = default);
}
