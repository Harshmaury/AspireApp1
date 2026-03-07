// src/Services/Identity/Identity.Tests/Domain/VerificationTokenTests.cs
using FluentAssertions;
using Identity.Domain.Entities;
using Identity.Domain.Exceptions;
using Xunit;

namespace Identity.Tests.Domain;

public sealed class VerificationTokenTests
{
    [Fact]
    public void Create_should_return_different_raw_and_hash_values()
    {
        var (token, raw) = VerificationToken.Create(Guid.NewGuid(), Guid.NewGuid(),
            TokenPurpose.EmailVerification);

        raw.Should().NotBeNullOrWhiteSpace();
        token.TokenHash.Should().NotBe(raw);
        token.TokenHash.Should().Be(VerificationToken.ComputeHash(raw));
    }

    [Fact]
    public void EmailVerification_token_should_expire_in_24_hours()
    {
        var (token, _) = VerificationToken.Create(Guid.NewGuid(), Guid.NewGuid(),
            TokenPurpose.EmailVerification);

        token.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddHours(24),
            precision: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void PasswordReset_token_should_expire_in_1_hour()
    {
        var (token, _) = VerificationToken.Create(Guid.NewGuid(), Guid.NewGuid(),
            TokenPurpose.PasswordReset);

        token.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddHours(1),
            precision: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void IsValid_should_be_true_for_fresh_unused_token()
    {
        var (token, _) = VerificationToken.Create(Guid.NewGuid(), Guid.NewGuid(),
            TokenPurpose.EmailVerification);

        token.IsValid.Should().BeTrue();
        token.IsExpired.Should().BeFalse();
        token.IsUsed.Should().BeFalse();
    }

    [Fact]
    public void MarkUsed_should_set_UsedAt()
    {
        var (token, _) = VerificationToken.Create(Guid.NewGuid(), Guid.NewGuid(),
            TokenPurpose.EmailVerification);

        token.MarkUsed();

        token.IsUsed.Should().BeTrue();
        token.UsedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkUsed_should_throw_when_already_used()
    {
        var (token, _) = VerificationToken.Create(Guid.NewGuid(), Guid.NewGuid(),
            TokenPurpose.EmailVerification);

        token.MarkUsed();
        var act = () => token.MarkUsed();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already been used*");
    }

    [Fact]
    public void ComputeHash_should_be_deterministic()
    {
        const string raw = "test-raw-token-abc123";
        var h1 = VerificationToken.ComputeHash(raw);
        var h2 = VerificationToken.ComputeHash(raw);
        h1.Should().Be(h2);
    }
}
