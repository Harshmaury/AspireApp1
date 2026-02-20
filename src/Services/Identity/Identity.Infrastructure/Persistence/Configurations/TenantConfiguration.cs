using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Slug).IsRequired().HasMaxLength(100);
        builder.HasIndex(t => t.Slug).IsUnique();
        builder.Property(t => t.LogoUrl).HasMaxLength(500);
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(t => t.Tier).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(t => t.SubscriptionStatus).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(t => t.MaxUsers).IsRequired().HasDefaultValue(100);
        builder.Property(t => t.Region).IsRequired().HasMaxLength(100).HasDefaultValue("default");
        builder.Property(t => t.FeaturesJson).IsRequired().HasDefaultValue("{}");
    }
}
