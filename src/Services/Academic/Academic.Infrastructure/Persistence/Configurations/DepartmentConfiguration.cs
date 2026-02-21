using Academic.Domain.Entities;
using Academic.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Academic.Infrastructure.Persistence.Configurations;
internal sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.TenantId).IsRequired();
        builder.Property(d => d.Name).IsRequired().HasMaxLength(200);
        builder.Property(d => d.Code).IsRequired().HasMaxLength(10);
        builder.Property(d => d.Description).HasMaxLength(500);
        builder.Property(d => d.EstablishedYear).IsRequired();
        builder.Property(d => d.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(d => d.CreatedAt).IsRequired();
        builder.HasIndex(d => new { d.TenantId, d.Code }).IsUnique();
        builder.HasIndex(d => d.TenantId);
        builder.Ignore(d => d.DomainEvents);
    }
}