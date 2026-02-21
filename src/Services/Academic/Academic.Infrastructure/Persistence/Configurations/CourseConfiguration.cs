using Academic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Academic.Infrastructure.Persistence.Configurations;
internal sealed class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("Courses");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.TenantId).IsRequired();
        builder.Property(c => c.DepartmentId).IsRequired();
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Code).IsRequired().HasMaxLength(20);
        builder.Property(c => c.Description).HasMaxLength(1000);
        builder.Property(c => c.Credits).IsRequired();
        builder.Property(c => c.LectureHours).IsRequired();
        builder.Property(c => c.TutorialHours).IsRequired();
        builder.Property(c => c.PracticalHours).IsRequired();
        builder.Property(c => c.CourseType).IsRequired().HasMaxLength(50);
        builder.Property(c => c.MaxEnrollment).IsRequired();
        builder.Property(c => c.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.HasIndex(c => new { c.TenantId, c.Code }).IsUnique();
        builder.HasIndex(c => new { c.TenantId, c.DepartmentId });
        builder.Ignore(c => c.DomainEvents);
    }
}