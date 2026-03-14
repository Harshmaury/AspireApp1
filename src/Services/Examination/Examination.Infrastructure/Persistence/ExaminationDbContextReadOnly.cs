// AGS-015: Read-only DbContext variant for SECONDARY region read routing.
// SaveChanges is intentionally blocked — this context is query-only.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using UMS.SharedKernel.Tenancy;

namespace Examination.Infrastructure.Persistence;

public sealed class ExaminationDbContextReadOnly : DbContext
{
    private readonly ITenantContext? _tenant;

    public ExaminationDbContextReadOnly(
        DbContextOptions<ExaminationDbContextReadOnly> options,
        ITenantContext? tenant = null)
        : base(options)
    {
        _tenant = tenant;
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        ChangeTracker.AutoDetectChangesEnabled = false;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(w =>
            w.Ignore(RelationalEventId.PendingModelChangesWarning));
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
        throw new InvalidOperationException(
            "ExaminationDbContextReadOnly is read-only. Use ExaminationDbContext for writes.");

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess, CancellationToken ct = default) =>
        throw new InvalidOperationException(
            "ExaminationDbContextReadOnly is read-only. Use ExaminationDbContext for writes.");
}
