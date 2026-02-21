using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Fee.Domain.Entities;
namespace Fee.Infrastructure.Persistence.Configurations;
public sealed class ScholarshipConfiguration : IEntityTypeConfiguration<Scholarship>
{
    public void Configure(EntityTypeBuilder<Scholarship> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.TenantId, e.StudentId });
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Amount).HasPrecision(10, 2);
        builder.Property(e => e.AcademicYear).HasMaxLength(10).IsRequired();
    }
}
