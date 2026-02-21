using Hostel.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Hostel.Infrastructure.Persistence.Configurations;
public sealed class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> b)
    {
        b.ToTable("Rooms");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired();
        b.Property(x => x.HostelId).IsRequired();
        b.Property(x => x.RoomNumber).IsRequired().HasMaxLength(20);
        b.Property(x => x.Floor).IsRequired();
        b.Property(x => x.Type).HasConversion<string>().IsRequired();
        b.Property(x => x.Capacity).IsRequired();
        b.Property(x => x.CurrentOccupancy).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().IsRequired();
        b.Property(x => x.CreatedAt).IsRequired();
        b.HasIndex(x => new { x.TenantId, x.HostelId });
        b.HasIndex(x => new { x.HostelId, x.RoomNumber }).IsUnique();
        b.Ignore(x => x.DomainEvents);
    }
}
