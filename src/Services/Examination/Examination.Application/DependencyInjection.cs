using Microsoft.Extensions.DependencyInjection;
using MediatR;
using System.Reflection;
namespace Examination.Application;
public static class DependencyInjection
{
    public static IServiceCollection AddExaminationApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        return services;
    }
}
