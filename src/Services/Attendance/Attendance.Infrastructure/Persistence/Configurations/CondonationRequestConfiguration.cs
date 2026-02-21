using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Attendance.Domain.Entities;
namespace Attendance.Infrastructure.Persistence.Configurations;
public sealed class CondonationRequestConfiguration : IEntityTypeConfiguration<CondonationRequest>
{
    public void Configure(EntityTypeBuilder<CondonationRequest> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.TenantId, e.StudentId, e.CourseId });
        builder.Property(e => e.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(e => e.DocumentUrl).HasMaxLength(500);
        builder.Property(e => e.ReviewNote).HasMaxLength(500);
        builder.Property(e => e.Status).HasConversion<string>();
    }
}
