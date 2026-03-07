// ═══════════════════════════════════════════════════════════════════════
// FILE 4 (NEW): Identity.Infrastructure/Persistence/Configurations/AuditLogConfiguration.cs
// ═══════════════════════════════════════════════════════════════════════
namespace Identity.Infrastructure.Persistence.Configurations;

using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Action).IsRequired().HasMaxLength(100);
        builder.Property(a => a.IpAddress).HasMaxLength(45);  // IPv6 max
        builder.Property(a => a.UserAgent).HasMaxLength(500);
        builder.Property(a => a.Details).HasMaxLength(2000);
        builder.Property(a => a.OccurredAt).IsRequired();
        builder.Property(a => a.TenantId).IsRequired();

        // Primary query pattern: all security events for a tenant in time order
        builder.HasIndex(a => new { a.TenantId, a.OccurredAt })
            .HasDatabaseName("IX_AuditLogs_TenantId_OccurredAt");

        // Secondary: all events for a specific user
        builder.HasIndex(a => new { a.UserId, a.OccurredAt })
            .HasDatabaseName("IX_AuditLogs_UserId_OccurredAt");
    }
}


