using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using FluentValidation;
namespace Faculty.Application;
public static class DependencyInjection
{
    public static IServiceCollection AddFacultyApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        return services;
    }
}
