using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Faculty.Application.Interfaces;
using Faculty.Infrastructure.Persistence;
using Faculty.Infrastructure.Persistence.Repositories;
namespace Faculty.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddFacultyInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<DomainEventDispatcherInterceptor>();
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
