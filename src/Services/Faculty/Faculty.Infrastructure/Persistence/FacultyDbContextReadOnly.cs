// AGS-015: Read-only DbContext variant for SECONDARY region read routing.
// Registered in DI so Aegis RegionAffinityRule detects 'ReadOnly' in the type name.
// Connection string 'ServiceDbReadOnly' is used when available (multi-region replica);
// falls back to 'FacultyDb' in single-region / Minikube environments.
// SaveChanges is intentionally blocked — this context is query-only.
using Microsoft.EntityFrameworkCore;
using UMS.SharedKernel.Tenancy;

namespace Faculty.Infrastructure.Persistence;

public sealed class FacultyDbContextReadOnly : DbContext
{
    private readonly ITenantContext? _tenant;

    public FacultyDbContextReadOnly(
        DbContextOptions<FacultyDbContextReadOnly> options,
        ITenantContext? tenant = null)
        : base(options)
    {
        _tenant = tenant;
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        ChangeTracker.AutoDetectChangesEnabled = false;
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
        throw new InvalidOperationException(
            "FacultyDbContextReadOnly is a read-only context. Use FacultyDbContext for writes.");

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess, CancellationToken ct = default) =>
        throw new InvalidOperationException(
            "FacultyDbContextReadOnly is a read-only context. Use FacultyDbContext for writes.");
}
