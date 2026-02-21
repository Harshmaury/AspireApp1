using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Notification.Application.Interfaces;
using Notification.Infrastructure.Channels;
using Notification.Infrastructure.Kafka;
using Notification.Infrastructure.Kafka.Consumers;
using Notification.Infrastructure.Persistence;
using Notification.Infrastructure.Persistence.Repositories;
namespace Notification.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddNotificationInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<NotificationDbContext>((sp, options) =>
            options.UseNpgsql(configuration.GetConnectionString("NotificationDb")));

        services.AddScoped<INotificationTemplateRepository, NotificationTemplateRepository>();
        services.AddScoped<INotificationLogRepository, NotificationLogRepository>();
        services.AddScoped<INotificationPreferenceRepository, NotificationPreferenceRepository>();

        // Channel registrations — Email primary, SMS stub
        services.AddScoped<INotificationChannel, EmailChannel>();

        // Kafka consumers — one per topic
        services.AddHostedService<IdentityEventsConsumer>();
        services.AddHostedService<StudentEventsConsumer>();
        services.AddHostedService<ExaminationEventsConsumer>();
        services.AddHostedService<FeeEventsConsumer>();
        services.AddHostedService<AcademicEventsConsumer>();

        return services;
    }

    public static async Task SeedDefaultTemplatesAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        await db.Database.MigrateAsync();
        if (!db.NotificationTemplates.Any())
        {
            db.NotificationTemplates.AddRange(Templates.DefaultTemplates.GetAll());
            await db.SaveChangesAsync();
        }
    }
}
