using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notification.Domain.Entities;
namespace Notification.Infrastructure.Persistence.Configurations;
public sealed class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.RecipientId);
        builder.HasIndex(e => e.Status);
        builder.Property(e => e.RecipientAddress).HasMaxLength(256).IsRequired();
        builder.Property(e => e.EventType).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Subject).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Body).HasMaxLength(5000).IsRequired();
        builder.Property(e => e.ErrorMessage).HasMaxLength(1000);
        builder.Property(e => e.Channel).HasConversion<string>();
        builder.Property(e => e.Status).HasConversion<string>();
    }
}
