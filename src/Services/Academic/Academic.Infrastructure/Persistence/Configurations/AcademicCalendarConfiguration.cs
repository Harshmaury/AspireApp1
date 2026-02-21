using Academic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Academic.Infrastructure.Persistence.Configurations;
internal sealed class AcademicCalendarConfiguration : IEntityTypeConfiguration<AcademicCalendar>
{
    public void Configure(EntityTypeBuilder<AcademicCalendar> builder)
    {
        builder.ToTable("AcademicCalendars");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.TenantId).IsRequired();
        builder.Property(a => a.AcademicYear).IsRequired().HasMaxLength(10);
        builder.Property(a => a.Semester).IsRequired();
        builder.Property(a => a.StartDate).IsRequired();
        builder.Property(a => a.EndDate).IsRequired();
        builder.Property(a => a.ExamStartDate).IsRequired();
        builder.Property(a => a.ExamEndDate).IsRequired();
        builder.Property(a => a.RegistrationOpenDate).IsRequired();
        builder.Property(a => a.RegistrationCloseDate).IsRequired();
        builder.Property(a => a.IsActive).IsRequired();
        builder.Property(a => a.CreatedAt).IsRequired();
        builder.HasIndex(a => new { a.TenantId, a.AcademicYear, a.Semester }).IsUnique();
        builder.HasIndex(a => new { a.TenantId, a.IsActive });
        builder.Ignore(a => a.DomainEvents);
    }
}