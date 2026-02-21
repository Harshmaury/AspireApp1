using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Attendance.Domain.Entities;
namespace Attendance.Infrastructure.Persistence.Configurations;
public sealed class AttendanceSummaryConfiguration : IEntityTypeConfiguration<AttendanceSummary>
{
    public void Configure(EntityTypeBuilder<AttendanceSummary> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.TenantId, e.StudentId, e.CourseId }).IsUnique();
        builder.Property(e => e.AcademicYear).HasMaxLength(10).IsRequired();
        builder.Property(e => e.Percentage).HasPrecision(5, 2);
    }
}
