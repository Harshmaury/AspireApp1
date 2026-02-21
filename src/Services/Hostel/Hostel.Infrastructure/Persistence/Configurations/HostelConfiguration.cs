using Hostel.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HostelEntity = Hostel.Domain.Entities.Hostel;
namespace Hostel.Infrastructure.Persistence.Configurations;
public sealed class HostelConfiguration : IEntityTypeConfiguration<HostelEntity>
{
    public void Configure(EntityTypeBuilder<HostelEntity> b)
    {
        b.ToTable("Hostels");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired();
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.Type).HasConversion<string>().IsRequired();
        b.Property(x => x.TotalRooms).IsRequired();
        b.Property(x => x.WardenName).IsRequired().HasMaxLength(200);
        b.Property(x => x.WardenContact).IsRequired().HasMaxLength(20);
        b.Property(x => x.IsActive).IsRequired();
        b.Property(x => x.CreatedAt).IsRequired();
        b.HasIndex(x => x.TenantId);
        b.Ignore(x => x.DomainEvents);
    }
}
