using Examination.Application.Interfaces;
using Examination.Infrastructure.Kafka;
using Examination.Infrastructure.Persistence;
using Examination.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Examination.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddExaminationInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Scoped matches IMediator lifetime — do NOT register as Singleton (captive dependency bug)
        services.AddScoped<DomainEventDispatcherInterceptor>();

        services.AddDbContext<ExaminationDbContext>((sp, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("ExaminationDb"));
            options.AddInterceptors(sp.GetRequiredService<DomainEventDispatcherInterceptor>());
        });

        services.AddDbContext<ExaminationDbContextReadOnly>((sp, options) =>
            options.UseNpgsql(
                configuration.GetConnectionString("ExaminationDbReadOnly")
                ?? configuration.GetConnectionString("ExaminationDb"))
                       .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
        services.AddScoped<ExaminationDbContextReadOnly, ExaminationDbContextReadOnly>();


        services.AddScoped<IExamScheduleRepository, ExamScheduleRepository>();
        services.AddScoped<IMarksEntryRepository, MarksEntryRepository>();
        services.AddScoped<IResultCardRepository, ResultCardRepository>();
        services.AddScoped<IHallTicketRepository, HallTicketRepository>();
        services.AddHostedService<ExaminationOutboxRelayService>();

        return services;
    }
}

