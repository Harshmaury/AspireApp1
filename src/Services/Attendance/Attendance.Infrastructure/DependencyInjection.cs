using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Attendance.Application.Interfaces;
using Attendance.Infrastructure.Persistence;
using Attendance.Infrastructure.Persistence.Repositories;
namespace Attendance.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddAttendanceInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<DomainEventDispatcherInterceptor>();
        services.AddDbContext<AttendanceDbContext>((sp, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("AttendanceDb"));
            options.AddInterceptors(sp.GetRequiredService<DomainEventDispatcherInterceptor>());
        });
        services.AddScoped<IAttendanceRecordRepository, AttendanceRecordRepository>();
        services.AddScoped<IAttendanceSummaryRepository, AttendanceSummaryRepository>();
        services.AddScoped<ICondonationRequestRepository, CondonationRequestRepository>();
        return services;
    }
}
