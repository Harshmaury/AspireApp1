using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace Examination.Infrastructure.Persistence;
public sealed class ExaminationDbContextFactory : IDesignTimeDbContextFactory<ExaminationDbContext>
{
    public ExaminationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ExaminationDbContext>()
            .UseNpgsql("Host=localhost;Database=ExaminationDb;Username=postgres;Password=postgres")
            .Options;
        return new ExaminationDbContext(options);
    }
}
