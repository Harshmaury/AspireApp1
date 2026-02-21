using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
namespace Fee.Application;
public static class DependencyInjection
{
    public static IServiceCollection AddFeeApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        return services;
    }
}
