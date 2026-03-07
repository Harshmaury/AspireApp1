// src/Services/Identity/Identity.Infrastructure/Persistence/Configurations/VerificationTokenConfiguration.cs
using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

internal sealed class VerificationTokenConfiguration : IEntityTypeConfiguration<VerificationToken>
{
    public void Configure(EntityTypeBuilder<VerificationToken> builder)
    {
        builder.ToTable("VerificationTokens");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.UserId).IsRequired();
        builder.Property(t => t.TenantId).IsRequired();
        builder.Property(t => t.TokenHash).IsRequired().HasMaxLength(64);
        builder.Property(t => t.Purpose).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(t => t.ExpiresAt).IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.IpAddress).HasMaxLength(45);

        // Primary lookup: hash + purpose (used during redemption)
        builder.HasIndex(t => new { t.TokenHash, t.Purpose })
            .IsUnique()
            .HasDatabaseName("IX_VerificationTokens_TokenHash_Purpose");

        // Secondary: invalidate all tokens for a user+purpose
        builder.HasIndex(t => new { t.UserId, t.Purpose })
            .HasDatabaseName("IX_VerificationTokens_UserId_Purpose");

        // Cleanup index: find expired unused tokens
        builder.HasIndex(t => new { t.ExpiresAt, t.UsedAt })
            .HasDatabaseName("IX_VerificationTokens_ExpiresAt_UsedAt");
    }
}
