using Microsoft.EntityFrameworkCore;
using Examination.Domain.Entities;
using Examination.Domain.Common;
namespace Examination.Infrastructure.Persistence;
public sealed class ExaminationDbContext : DbContext
{
    public DbSet<ExamSchedule> ExamSchedules => Set<ExamSchedule>();
    public DbSet<HallTicket> HallTickets => Set<HallTicket>();
    public DbSet<MarksEntry> MarksEntries => Set<MarksEntry>();
    public DbSet<ResultCard> ResultCards => Set<ResultCard>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public ExaminationDbContext(DbContextOptions<ExaminationDbContext> options) : base(options) { }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ExaminationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
