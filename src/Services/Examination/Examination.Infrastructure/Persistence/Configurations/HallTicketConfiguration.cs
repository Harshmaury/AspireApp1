using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Examination.Domain.Entities;
namespace Examination.Infrastructure.Persistence.Configurations;
public sealed class HallTicketConfiguration : IEntityTypeConfiguration<HallTicket>
{
    public void Configure(EntityTypeBuilder<HallTicket> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.TenantId, e.StudentId, e.ExamScheduleId }).IsUnique();
        builder.Property(e => e.RollNumber).HasMaxLength(50).IsRequired();
        builder.Property(e => e.SeatNumber).HasMaxLength(20).IsRequired();
        builder.Property(e => e.IneligibilityReason).HasMaxLength(500);
    }
}
