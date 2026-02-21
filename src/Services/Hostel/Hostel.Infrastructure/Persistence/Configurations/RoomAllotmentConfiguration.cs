using Hostel.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Hostel.Infrastructure.Persistence.Configurations;
public sealed class RoomAllotmentConfiguration : IEntityTypeConfiguration<RoomAllotment>
{
    public void Configure(EntityTypeBuilder<RoomAllotment> b)
    {
        b.ToTable("RoomAllotments");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired();
        b.Property(x => x.StudentId).IsRequired();
        b.Property(x => x.RoomId).IsRequired();
        b.Property(x => x.HostelId).IsRequired();
        b.Property(x => x.AcademicYear).IsRequired().HasMaxLength(10);
        b.Property(x => x.BedNumber).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().IsRequired();
        b.Property(x => x.AllottedAt).IsRequired();
        b.Property(x => x.VacatedAt);
        b.HasIndex(x => new { x.TenantId, x.StudentId, x.AcademicYear });
        b.HasIndex(x => new { x.TenantId, x.RoomId, x.AcademicYear });
        b.Ignore(x => x.DomainEvents);
    }
}
