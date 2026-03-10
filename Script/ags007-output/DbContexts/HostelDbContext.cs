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

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.ApplyConfigurationsFromAssembly(typeof(HostelDbContext).Assembly);
        base.OnModelCreating(mb);

        if (_tenant?.IsResolved == true)
        {
            var tid = _tenant.TenantId;
            mb.Entity<HostelEntity>()   .HasQueryFilter(e => e.TenantId == tid);
            mb.Entity<Room>()           .HasQueryFilter(e => e.TenantId == tid);
            mb.Entity<RoomAllotment>()  .HasQueryFilter(e => e.TenantId == tid);
            mb.Entity<HostelComplaint>().HasQueryFilter(e => e.TenantId == tid);
        }
    }
}
