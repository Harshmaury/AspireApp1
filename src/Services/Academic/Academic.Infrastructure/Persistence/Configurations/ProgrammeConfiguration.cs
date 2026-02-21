using Academic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Academic.Infrastructure.Persistence.Configurations;
internal sealed class ProgrammeConfiguration : IEntityTypeConfiguration<Programme>
{
    public void Configure(EntityTypeBuilder<Programme> builder)
    {
        builder.ToTable("Programmes");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.TenantId).IsRequired();
        builder.Property(p => p.DepartmentId).IsRequired();
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Code).IsRequired().HasMaxLength(20);
        builder.Property(p => p.Degree).IsRequired().HasMaxLength(50);
        builder.Property(p => p.DurationYears).IsRequired();
        builder.Property(p => p.TotalCredits).IsRequired();
        builder.Property(p => p.IntakeCapacity).IsRequired();
        builder.Property(p => p.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.HasIndex(p => new { p.TenantId, p.Code }).IsUnique();
        builder.HasIndex(p => new { p.TenantId, p.DepartmentId });
        builder.Ignore(p => p.DomainEvents);
    }
}