using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Examination.Application.Interfaces;
using Examination.Infrastructure.Kafka;
using Examination.Infrastructure.Persistence;
using Examination.Infrastructure.Persistence.Repositories;
namespace Examination.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddExaminationInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<DomainEventDispatcherInterceptor>();
        services.AddDbContext<ExaminationDbContext>((sp, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("ExaminationDb"));
            options.AddInterceptors(sp.GetRequiredService<DomainEventDispatcherInterceptor>());
        });
        services.AddScoped<IExamScheduleRepository, ExamScheduleRepository>();
        services.AddScoped<IMarksEntryRepository, MarksEntryRepository>();
        services.AddScoped<IResultCardRepository, ResultCardRepository>();
        services.AddScoped<IHallTicketRepository, HallTicketRepository>();
        services.AddHostedService<ExaminationOutboxRelayService>();
        return services;
    }
}
