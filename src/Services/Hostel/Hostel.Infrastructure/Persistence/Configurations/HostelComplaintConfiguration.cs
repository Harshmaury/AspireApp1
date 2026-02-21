using Hostel.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Hostel.Infrastructure.Persistence.Configurations;
public sealed class HostelComplaintConfiguration : IEntityTypeConfiguration<HostelComplaint>
{
    public void Configure(EntityTypeBuilder<HostelComplaint> b)
    {
        b.ToTable("HostelComplaints");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired();
        b.Property(x => x.StudentId).IsRequired();
        b.Property(x => x.HostelId).IsRequired();
        b.Property(x => x.Category).HasConversion<string>().IsRequired();
        b.Property(x => x.Description).IsRequired().HasMaxLength(1000);
        b.Property(x => x.Status).HasConversion<string>().IsRequired();
        b.Property(x => x.ResolutionNote).HasMaxLength(1000);
        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.ResolvedAt);
        b.HasIndex(x => new { x.TenantId, x.Status });
        b.HasIndex(x => new { x.TenantId, x.StudentId });
        b.Ignore(x => x.DomainEvents);
    }
}
