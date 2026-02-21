using Hostel.Domain.Common;
using Hostel.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using HostelEntity = Hostel.Domain.Entities.Hostel;
namespace Hostel.Infrastructure.Persistence;
public sealed class HostelDbContext(DbContextOptions<HostelDbContext> options) : DbContext(options)
{
    public DbSet<HostelEntity> Hostels => Set<HostelEntity>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<RoomAllotment> RoomAllotments => Set<RoomAllotment>();
    public DbSet<HostelComplaint> HostelComplaints => Set<HostelComplaint>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.ApplyConfigurationsFromAssembly(typeof(HostelDbContext).Assembly);
        base.OnModelCreating(mb);
    }
}
