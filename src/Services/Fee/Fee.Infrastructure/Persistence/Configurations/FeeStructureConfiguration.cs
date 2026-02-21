using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Fee.Domain.Entities;
namespace Fee.Infrastructure.Persistence.Configurations;
public sealed class FeeStructureConfiguration : IEntityTypeConfiguration<FeeStructure>
{
    public void Configure(EntityTypeBuilder<FeeStructure> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.TenantId, e.ProgrammeId, e.AcademicYear, e.Semester }).IsUnique();
        builder.Property(e => e.AcademicYear).HasMaxLength(10).IsRequired();
        builder.Property(e => e.TuitionFee).HasPrecision(10, 2);
        builder.Property(e => e.ExamFee).HasPrecision(10, 2);
        builder.Property(e => e.DevelopmentFee).HasPrecision(10, 2);
        builder.Property(e => e.MedicalFee).HasPrecision(10, 2);
        builder.Property(e => e.HostelFee).HasPrecision(10, 2);
        builder.Property(e => e.MessFee).HasPrecision(10, 2);
        builder.Property(e => e.TotalFee).HasPrecision(10, 2);
    }
}
