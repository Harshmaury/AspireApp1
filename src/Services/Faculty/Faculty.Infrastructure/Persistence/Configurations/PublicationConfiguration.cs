using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Faculty.Domain.Entities;
namespace Faculty.Infrastructure.Persistence.Configurations;
public sealed class PublicationConfiguration : IEntityTypeConfiguration<Publication>
{
    public void Configure(EntityTypeBuilder<Publication> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.TenantId, e.FacultyId });
        builder.Property(e => e.Title).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Journal).HasMaxLength(300).IsRequired();
        builder.Property(e => e.DOI).HasMaxLength(200);
        builder.Property(e => e.Type).HasConversion<string>();
    }
}
