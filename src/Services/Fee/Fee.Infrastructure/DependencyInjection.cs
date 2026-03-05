using Fee.Application.Interfaces;
using Fee.Infrastructure.Kafka;
using Fee.Infrastructure.Persistence;
using Fee.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fee.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddFeeInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Scoped matches IMediator lifetime — do NOT register as Singleton (captive dependency bug)
        services.AddScoped<DomainEventDispatcherInterceptor>();

        services.AddDbContext<FeeDbContext>((sp, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("FeeDb"));
            options.AddInterceptors(sp.GetRequiredService<DomainEventDispatcherInterceptor>());
        });

        services.AddScoped<IFeeStructureRepository, FeeStructureRepository>();
        services.AddScoped<IFeePaymentRepository, FeePaymentRepository>();
        services.AddScoped<IScholarshipRepository, ScholarshipRepository>();
        services.AddHostedService<FeeOutboxRelayService>();

        return services;
    }
}
