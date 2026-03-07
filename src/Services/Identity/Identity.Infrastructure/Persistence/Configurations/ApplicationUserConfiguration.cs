// src/Services/Identity/Identity.Infrastructure/Persistence/Configurations/ApplicationUserConfiguration.cs
using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("Users");

        builder.Ignore(u => u.DomainEvents);

        builder.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.LastName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(u => u.CreatedAt).IsRequired();
        builder.Property(u => u.TenantId).IsRequired();
        builder.Property(u => u.RowVersion).IsRowVersion();

        // BUG-003 FIX: Override ASP.NET Identity's default GLOBAL unique index on
        // NormalizedUserName. Since UserName = Email, a global unique index would
        // prevent the same email from existing in two different tenants — breaking
        // multi-tenancy. We make it non-unique here; uniqueness is enforced by the
        // composite (TenantId, NormalizedEmail) index below.
        builder.HasIndex(u => u.NormalizedUserName)
            .HasDatabaseName("IX_Users_NormalizedUserName")
            .IsUnique(false);

        // Unique email scoped to tenant — this is the real uniqueness constraint
        builder.HasIndex(u => new { u.TenantId, u.NormalizedEmail })
            .IsUnique()
            .HasDatabaseName("IX_Users_TenantId_NormalizedEmail");

        builder.HasIndex(u => u.TenantId)
            .HasDatabaseName("IX_Users_TenantId");

        // Keep non-unique NormalizedEmail index for Identity lookups
        builder.HasIndex(u => u.NormalizedEmail)
            .HasDatabaseName("EmailIndex")
            .IsUnique(false);

        builder.HasOne(u => u.Tenant)
            .WithMany()
            .HasForeignKey(u => u.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}