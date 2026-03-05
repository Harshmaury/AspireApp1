using Examination.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Examination.Infrastructure.Persistence.Configurations;
public sealed class ResultCardConfiguration : IEntityTypeConfiguration<ResultCard>
{
    public void Configure(EntityTypeBuilder<ResultCard> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.TenantId, e.StudentId, e.AcademicYear, e.Semester }).IsUnique();
        builder.Property(e => e.AcademicYear).HasMaxLength(10).IsRequired();
        builder.Property(e => e.SGPA).HasPrecision(4, 2);
        builder.Property(e => e.CGPA).HasPrecision(4, 2);
    }
}
