using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using UMS.SharedKernel.Application;

namespace Examination.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddExaminationApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            // FIX EXM-3/A3: ValidationBehavior was never wired into the MediatR pipeline.
            // Validators existed but were silently never called.
            // Pattern copied from Faculty.Application (gold standard).
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        return services;
    }
}

