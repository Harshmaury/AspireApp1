// AGS-015: Read-only DbContext variant for SECONDARY region read routing.
// Registered in DI so Aegis RegionAffinityRule detects 'ReadOnly' in the type name.
// Connection string 'ServiceDbReadOnly' is used when available (multi-region replica);
// falls back to 'HostelDb' in single-region / Minikube environments.
// SaveChanges is intentionally blocked — this context is query-only.
using Microsoft.EntityFrameworkCore;
using UMS.SharedKernel.Tenancy;

namespace Hostel.Infrastructure.Persistence;

public sealed class HostelDbContextReadOnly : DbContext
{
    private readonly ITenantContext? _tenant;

    public HostelDbContextReadOnly(
        DbContextOptions<HostelDbContextReadOnly> options,
        ITenantContext? tenant = null)
        : base(options)
    {
        _tenant = tenant;
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        ChangeTracker.AutoDetectChangesEnabled = false;
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
        throw new InvalidOperationException(
            "HostelDbContextReadOnly is a read-only context. Use HostelDbContext for writes.");

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess, CancellationToken ct = default) =>
        throw new InvalidOperationException(
            "HostelDbContextReadOnly is a read-only context. Use HostelDbContext for writes.");
}
