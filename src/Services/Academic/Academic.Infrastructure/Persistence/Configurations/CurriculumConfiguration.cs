using Academic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Academic.Infrastructure.Persistence.Configurations;
internal sealed class CurriculumConfiguration : IEntityTypeConfiguration<Curriculum>
{
    public void Configure(EntityTypeBuilder<Curriculum> builder)
    {
        builder.ToTable("Curricula");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.TenantId).IsRequired();
        builder.Property(c => c.ProgrammeId).IsRequired();
        builder.Property(c => c.CourseId).IsRequired();
        builder.Property(c => c.Semester).IsRequired();
        builder.Property(c => c.IsElective).IsRequired();
        builder.Property(c => c.IsOptional).IsRequired();
        builder.Property(c => c.Version).IsRequired().HasMaxLength(10);
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.HasIndex(c => new { c.TenantId, c.ProgrammeId, c.CourseId, c.Version }).IsUnique();
        builder.Ignore(c => c.DomainEvents);
    }
}