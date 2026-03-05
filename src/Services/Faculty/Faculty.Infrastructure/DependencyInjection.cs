using Faculty.Application.Interfaces;
using Faculty.Infrastructure.Persistence;
using Faculty.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Faculty.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddFacultyInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Scoped - NOT Singleton. DomainEventDispatcherInterceptor depends on
        // IMediator which is Scoped. A Singleton interceptor holding a Scoped
        // dependency causes a captive dependency - the first request's IMediator
        // is frozen inside the interceptor for the lifetime of the process.
        services.AddScoped<DomainEventDispatcherInterceptor>();

        services.AddDbContext<FacultyDbContext>((sp, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("FacultyDb"));
            options.AddInterceptors(sp.GetRequiredService<DomainEventDispatcherInterceptor>());
        });

        services.AddScoped<IFacultyRepository, FacultyRepository>();
        services.AddScoped<ICourseAssignmentRepository, CourseAssignmentRepository>();
        services.AddScoped<IPublicationRepository, PublicationRepository>();

        return services;
    }
}