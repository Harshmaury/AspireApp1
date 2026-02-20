using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Student.Domain.Common;

namespace Student.Infrastructure.Persistence.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.EventType).IsRequired().HasMaxLength(200);
        builder.Property(o => o.Payload).IsRequired();
        builder.Property(o => o.CreatedAt).IsRequired();
        builder.Property(o => o.Error).HasMaxLength(2000);
        builder.HasIndex(o => o.ProcessedAt);
    }
}
