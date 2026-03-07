// src/Services/Identity/Identity.Domain/Entities/VerificationToken.cs
using System.Security.Cryptography;

namespace Identity.Domain.Entities;

public sealed class VerificationToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid TenantId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public TokenPurpose Purpose { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? UsedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string? IpAddress { get; private set; }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsUsed => UsedAt.HasValue;
    public bool IsValid => !IsExpired && !IsUsed;

    private VerificationToken() { }

    public static (VerificationToken Token, string RawToken) Create(
        Guid userId,
        Guid tenantId,
        TokenPurpose purpose,
        string? ipAddress = null)
    {
        // 32 cryptographically random bytes = 256 bits of entropy
        var rawBytes = RandomNumberGenerator.GetBytes(32);
        var rawToken = Convert.ToBase64String(rawBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        var hash = ComputeHash(rawToken);

        var expiry = purpose switch
        {
            TokenPurpose.EmailVerification => TimeSpan.FromHours(24),
            TokenPurpose.PasswordReset     => TimeSpan.FromHours(1),
            TokenPurpose.MfaSetup          => TimeSpan.FromMinutes(15),
            _                              => TimeSpan.FromHours(1)
        };

        var token = new VerificationToken
        {
            Id        = Guid.NewGuid(),
            UserId    = userId,
            TenantId  = tenantId,
            TokenHash = hash,
            Purpose   = purpose,
            ExpiresAt = DateTime.UtcNow.Add(expiry),
            CreatedAt = DateTime.UtcNow,
            IpAddress = ipAddress
        };

        return (token, rawToken);
    }

    public void MarkUsed()
    {
        if (IsUsed)
            throw new InvalidOperationException("Token has already been used.");
        if (IsExpired)
            throw new InvalidOperationException("Token has expired.");
        UsedAt = DateTime.UtcNow;
    }

    public static string ComputeHash(string rawToken)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(rawToken);
        var hash  = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
