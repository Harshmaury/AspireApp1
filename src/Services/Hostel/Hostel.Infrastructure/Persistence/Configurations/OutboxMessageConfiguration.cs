using Hostel.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Hostel.Infrastructure.Persistence.Configurations;
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> b)
    {
        b.ToTable("OutboxMessages");
        b.HasKey(x => x.Id);
        b.Property(x => x.EventType).IsRequired().HasMaxLength(200);
        b.Property(x => x.Payload).IsRequired();
        b.Property(x => x.CreatedAt).IsRequired();
        b.HasIndex(x => x.ProcessedAt);
    }
}
