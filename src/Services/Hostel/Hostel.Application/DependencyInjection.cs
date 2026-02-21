using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
namespace Hostel.Application;
public static class DependencyInjection
{
    public static IServiceCollection AddHostelApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        return services;
    }
}
