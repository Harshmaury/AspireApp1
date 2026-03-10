using Hostel.Domain.Common;
using Hostel.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using UMS.SharedKernel.Tenancy;
using HostelEntity = Hostel.Domain.Entities.Hostel;

namespace Hostel.Infrastructure.Persistence;

public sealed class HostelDbContext : DbContext
{
    private readonly ITenantContext? _tenant;

    public HostelDbContext(
        DbContextOptions<HostelDbContext> options,
        ITenantContext? tenant = null)
        : base(options)
    {
        _tenant = tenant;
    }

    public DbSet<HostelEntity>    Hostels          => Set<HostelEntity>();
    public DbSet<Room>            Rooms            => Set<Room>();
    public DbSet<RoomAllotment>   RoomAllotments   => Set<RoomAllotment>();
    public DbSet<HostelComplaint> HostelComplaints => Set<HostelComplaint>();
    public DbSet<OutboxMessage>   OutboxMessages   => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HostelDbContext).Assembly);
        base.OnModelCreating(modelBuilder);

        // Tenant isolation — filter evaluated per-query via _tenant field reference
        // EF Core evaluates this per DbContext instance, not once at model-build time
        modelBuilder.Entity<HostelEntity>().HasQueryFilter(
            e => _tenant == null || !_tenant.IsResolved || e.TenantId == _tenant.TenantId);
        modelBuilder.Entity<Room>().HasQueryFilter(
            e => _tenant == null || !_tenant.IsResolved || e.TenantId == _tenant.TenantId);
        modelBuilder.Entity<RoomAllotment>().HasQueryFilter(
            e => _tenant == null || !_tenant.IsResolved || e.TenantId == _tenant.TenantId);
        modelBuilder.Entity<HostelComplaint>().HasQueryFilter(
            e => _tenant == null || !_tenant.IsResolved || e.TenantId == _tenant.TenantId);
    }
}


