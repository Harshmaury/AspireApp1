using Academic.Application.Interfaces;
using Academic.Infrastructure.Kafka;
using Academic.Infrastructure.Persistence;
using Academic.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace Academic.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<DomainEventDispatcherInterceptor>();
        services.AddDbContext<AcademicDbContext>((sp, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("AcademicDb"),
                npgsql => npgsql.MigrationsAssembly(typeof(AcademicDbContext).Assembly.FullName));
            options.AddInterceptors(sp.GetRequiredService<DomainEventDispatcherInterceptor>());
        });
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IProgrammeRepository, ProgrammeRepository>();
        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<ICurriculumRepository, CurriculumRepository>();
        services.AddScoped<IAcademicCalendarRepository, AcademicCalendarRepository>();
        services.AddHostedService<AcademicOutboxRelayService>();
        return services;
    }
}