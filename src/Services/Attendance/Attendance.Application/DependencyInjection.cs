using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using UMS.SharedKernel.Application;

namespace Attendance.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAttendanceApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            // FIX ATT-3: ValidationBehavior was registered but never wired into the
            // MediatR pipeline. MarkAttendanceCommandValidator (7-day backdating rule)
            // was silently never called. Pattern copied from Faculty.Application.
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        return services;
    }
}

