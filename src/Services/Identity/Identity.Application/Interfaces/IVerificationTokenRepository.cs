// src/Services/Identity/Identity.Application/Interfaces/IVerificationTokenRepository.cs
using Identity.Domain.Entities;

namespace Identity.Application.Interfaces;

public interface IVerificationTokenRepository
{
    Task CreateAsync(VerificationToken token, CancellationToken ct = default);
    Task<VerificationToken?> FindByHashAsync(string tokenHash, TokenPurpose purpose, CancellationToken ct = default);
    Task UpdateAsync(VerificationToken token, CancellationToken ct = default);
    Task InvalidateAllForUserAsync(Guid userId, TokenPurpose purpose, CancellationToken ct = default);
}

