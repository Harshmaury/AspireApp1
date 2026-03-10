using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using UMS.SharedKernel.Application;

namespace Academic.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            // FIX A3: ValidationBehavior was registered but never wired into the
            // MediatR pipeline — validators were silently never called.
            // Pattern copied from Faculty.Application (gold standard).
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        return services;
    }
}

