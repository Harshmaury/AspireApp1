using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        // AGS-015: ReadOnly variant for SECONDARY region read routing.
        services.AddDbContext<NotificationDbContextReadOnly>((sp, options) =>
            options.UseNpgsql(
                configuration.GetConnectionString("NotificationDbReadOnly")
                ?? configuration.GetConnectionString("NotificationDb"))
                   .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
        services.AddScoped<NotificationDbContextReadOnly, NotificationDbContextReadOnly>();

        services.AddScoped<INotificationTemplateRepository, NotificationTemplateRepository>();
        services.AddScoped<INotificationLogRepository, NotificationLogRepository>();
        services.AddScoped<INotificationPreferenceRepository, NotificationPreferenceRepository>();

        // FIX NOT-1: SmsChannel was implemented and correct but never registered in DI.
        // DispatchAsync calls with channel = NotificationChannel.SMS were silently dropped.
        services.AddScoped<INotificationChannel, EmailChannel>();
        services.AddScoped<INotificationChannel, SmsChannel>();

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
