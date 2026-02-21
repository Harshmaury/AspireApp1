using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Fee.Application.Interfaces;
using Fee.Infrastructure.Kafka;
using Fee.Infrastructure.Persistence;
using Fee.Infrastructure.Persistence.Repositories;
namespace Fee.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddFeeInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<DomainEventDispatcherInterceptor>();
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
