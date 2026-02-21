using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Examination.Domain.Entities;
namespace Examination.Infrastructure.Persistence.Configurations;
public sealed class MarksEntryConfiguration : IEntityTypeConfiguration<MarksEntry>
{
    public void Configure(EntityTypeBuilder<MarksEntry> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.TenantId, e.StudentId, e.ExamScheduleId }).IsUnique();
        builder.Property(e => e.MarksObtained).HasPrecision(5, 2);
        builder.Property(e => e.GradePoint).HasPrecision(4, 2);
        builder.Property(e => e.Grade).HasMaxLength(5).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>();
    }
}
