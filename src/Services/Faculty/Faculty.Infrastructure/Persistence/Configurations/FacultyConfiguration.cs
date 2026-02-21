using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FacultyEntity = Faculty.Domain.Entities.Faculty;
namespace Faculty.Infrastructure.Persistence.Configurations;
public sealed class FacultyConfiguration : IEntityTypeConfiguration<FacultyEntity>
{
    public void Configure(EntityTypeBuilder<FacultyEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.TenantId, e.EmployeeId }).IsUnique();
        builder.HasIndex(e => new { e.TenantId, e.UserId }).IsUnique();
        builder.Property(e => e.EmployeeId).HasMaxLength(50).IsRequired();
        builder.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.LastName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Email).HasMaxLength(256).IsRequired();
        builder.Property(e => e.Specialization).HasMaxLength(500).IsRequired();
        builder.Property(e => e.HighestQualification).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Designation).HasConversion<string>();
        builder.Property(e => e.Status).HasConversion<string>();
    }
}
