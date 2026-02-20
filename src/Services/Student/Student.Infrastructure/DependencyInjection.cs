using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Student.Application.Interfaces;
using Student.Infrastructure.Persistence;
using Student.Infrastructure.Persistence.Repositories;

namespace Student.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<StudentDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("StudentDb"),
                npgsql => npgsql.MigrationsAssembly(
                    typeof(StudentDbContext).Assembly.FullName)));

        services.AddScoped<IStudentRepository, StudentRepository>();

        return services;
    }
}
