using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace Attendance.Infrastructure.Persistence;
public sealed class AttendanceDbContextFactory : IDesignTimeDbContextFactory<AttendanceDbContext>
{
    public AttendanceDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseNpgsql("Host=localhost;Database=AttendanceDb;Username=postgres;Password=postgres")
            .Options;
        return new AttendanceDbContext(options);
    }
}
