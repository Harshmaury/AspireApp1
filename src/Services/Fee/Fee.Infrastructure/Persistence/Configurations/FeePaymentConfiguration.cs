using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Fee.Domain.Entities;
namespace Fee.Infrastructure.Persistence.Configurations;
public sealed class FeePaymentConfiguration : IEntityTypeConfiguration<FeePayment>
{
    public void Configure(EntityTypeBuilder<FeePayment> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.TenantId, e.StudentId });
        builder.Property(e => e.AmountPaid).HasPrecision(10, 2);
        builder.Property(e => e.PaymentMode).HasConversion<string>();
        builder.Property(e => e.Status).HasConversion<string>();
        builder.Property(e => e.ReceiptNumber).HasMaxLength(50).IsRequired();
        builder.Property(e => e.TransactionId).HasMaxLength(200);
        builder.Property(e => e.Gateway).HasMaxLength(50);
    }
}
