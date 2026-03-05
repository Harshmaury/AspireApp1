using Faculty.Application.Behaviours;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Faculty.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddFacultyApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            // Runs before every handler. If no validator exists for a command,
            // passes straight through. If validation fails, throws before
            // any repository call is made - no partial state.
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}