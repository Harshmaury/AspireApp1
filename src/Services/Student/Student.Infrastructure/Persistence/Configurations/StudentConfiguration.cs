using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Student.Domain.Entities;
using Student.Domain.Enums;

namespace Student.Infrastructure.Persistence.Configurations;

internal sealed class StudentConfiguration : IEntityTypeConfiguration<StudentAggregate>
{
    public void Configure(EntityTypeBuilder<StudentAggregate> builder)
    {
        builder.ToTable("Students");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.TenantId).IsRequired();
        builder.Property(s => s.UserId).IsRequired();
        builder.Property(s => s.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(s => s.LastName).IsRequired().HasMaxLength(100);
        builder.Property(s => s.Email).IsRequired().HasMaxLength(256);
        builder.Property(s => s.StudentNumber).IsRequired().HasMaxLength(50);
        builder.Property(s => s.Status).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(s => s.SuspensionReason).HasMaxLength(500);
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.HasIndex(s => new { s.TenantId, s.UserId }).IsUnique();
        builder.HasIndex(s => s.StudentNumber).IsUnique();
        builder.HasIndex(s => new { s.TenantId, s.Status });
        builder.Ignore(s => s.DomainEvents);
    }
}
