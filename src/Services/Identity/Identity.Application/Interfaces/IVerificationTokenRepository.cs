// UMS — University Management System
// Key:     UMS-IDENTITY-P2-018
// Service: Identity
// Layer:   Application / Interfaces
// src/Services/Identity/Identity.Application/Interfaces/IVerificationTokenRepository.cs
namespace Identity.Application.Interfaces;

using Identity.Domain.Entities;

public interface IVerificationTokenRepository
{
    Task CreateAsync(VerificationToken token, CancellationToken ct = default);

    Task<VerificationToken?> FindByHashAsync(
        string tokenHash, TokenPurpose purpose, CancellationToken ct = default);

    Task UpdateAsync(VerificationToken token, CancellationToken ct = default);

    /// <summary>
    /// Marks all active (unused, unexpired) tokens for a user+purpose as used
    /// before issuing a new one — prevents token accumulation and replay.
    /// </summary>
    Task InvalidateByUserAsync(
        Guid userId, TokenPurpose purpose, CancellationToken ct = default);
}
