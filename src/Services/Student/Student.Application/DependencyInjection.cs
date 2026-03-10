using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using UMS.SharedKernel.Application;
namespace Student.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            // ValidationBehavior runs before every handler.
            // If no validator exists for a command, the pipeline passes straight through.
            // If a validator exists and fails, ValidationException is thrown before
            // the handler executes - no partial state, no repository call made.
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // Registers CreateStudentCommandValidator, UpdateStudentCommandValidator,
        // SuspendStudentCommandValidator - all discovered automatically.
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
