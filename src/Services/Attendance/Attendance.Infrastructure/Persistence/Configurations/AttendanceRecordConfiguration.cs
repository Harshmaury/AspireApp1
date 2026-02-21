using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Attendance.Domain.Entities;
namespace Attendance.Infrastructure.Persistence.Configurations;
public sealed class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.TenantId, e.StudentId, e.CourseId, e.Date }).IsUnique();
        builder.Property(e => e.AcademicYear).HasMaxLength(10).IsRequired();
        builder.Property(e => e.ClassType).HasConversion<string>();
    }
}
