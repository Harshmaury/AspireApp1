using Confluent.Kafka;
using Hostel.Application.Interfaces;
using Hostel.Infrastructure.Kafka;
using Hostel.Infrastructure.Persistence;
using Hostel.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hostel.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddHostelInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<DomainEventDispatcherInterceptor>();

        services.AddDbContext<HostelDbContext>((sp, opts) =>
        {
            opts.UseNpgsql(config.GetConnectionString("HostelDb"))
                .AddInterceptors(sp.GetRequiredService<DomainEventDispatcherInterceptor>());
        });

        services.AddDbContext<HostelDbContextReadOnly>((sp, options) =>
            options.UseNpgsql(
                config.GetConnectionString("HostelDbReadOnly")
                ?? config.GetConnectionString("HostelDb"))
                   .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
        services.AddScoped<HostelDbContextReadOnly, HostelDbContextReadOnly>();

        services.AddScoped<IHostelRepository, HostelRepository>();
        services.AddScoped<IHostelUnitOfWork, HostelUnitOfWork>();

        var kafkaCfg = new ProducerConfig
        {
            BootstrapServers = config.GetConnectionString("kafka") ?? "localhost:9092",
            Acks = Acks.Leader
        };
        services.AddSingleton<IProducer<string, string>>(
            new ProducerBuilder<string, string>(kafkaCfg).Build());

        services.AddHostedService<HostelOutboxRelayService>();
        return services;
    }
}
