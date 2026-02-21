using Microsoft.Extensions.DependencyInjection;
using Notification.Application.Services;
namespace Notification.Application;
public static class DependencyInjection
{
    public static IServiceCollection AddNotificationApplication(this IServiceCollection services)
    {
        services.AddScoped<NotificationDispatcher>();
        return services;
    }
}
