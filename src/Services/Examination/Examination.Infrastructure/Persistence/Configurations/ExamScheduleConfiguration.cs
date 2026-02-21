using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Examination.Domain.Entities;
namespace Examination.Infrastructure.Persistence.Configurations;
public sealed class ExamScheduleConfiguration : IEntityTypeConfiguration<ExamSchedule>
{
    public void Configure(EntityTypeBuilder<ExamSchedule> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.TenantId, e.CourseId });
        builder.Property(e => e.AcademicYear).HasMaxLength(10).IsRequired();
        builder.Property(e => e.Venue).HasMaxLength(200).IsRequired();
        builder.Property(e => e.ExamType).HasConversion<string>();
        builder.Property(e => e.Status).HasConversion<string>();
    }
}
