// src/Services/Identity/Identity.Infrastructure/Persistence/ApplicationDbContext.cs
using Identity.Domain.Common;
using Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UMS.SharedKernel.Tenancy;

namespace Identity.Infrastructure.Persistence;

public sealed class ApplicationDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    private readonly ITenantContext? _tenantContext;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ITenantContext? tenantContext = null)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Tenant>             Tenants            => Set<Tenant>();
    public DbSet<OutboxMessage>      OutboxMessages     => Set<OutboxMessage>();
    public DbSet<AuditLog>           AuditLogs          => Set<AuditLog>();
    public DbSet<VerificationToken>  VerificationTokens => Set<VerificationToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // AGS-007: Scope all user queries to the current tenant.
        // Nullable check allows design-time tooling (migrations) and seeder to work.
        if (_tenantContext?.IsResolved == true)
        {
            var tid = _tenantContext.TenantId;
            modelBuilder.Entity<ApplicationUser>()
                .HasQueryFilter(u => u.TenantId == tid);
        }
    }
}

