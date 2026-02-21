using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notification.Domain.Entities;
using Notification.Domain.Enums;
namespace Notification.Infrastructure.Persistence.Configurations;
public sealed class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.TenantId, e.EventType, e.Channel }).IsUnique();
        builder.Property(e => e.EventType).HasMaxLength(100).IsRequired();
        builder.Property(e => e.SubjectTemplate).HasMaxLength(500).IsRequired();
        builder.Property(e => e.BodyTemplate).HasMaxLength(5000).IsRequired();
        builder.Property(e => e.Channel).HasConversion<string>();
    }
}
