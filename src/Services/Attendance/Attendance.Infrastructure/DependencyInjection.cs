using Attendance.Application.Interfaces;
using Attendance.Infrastructure.Persistence;
using Attendance.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UMS.SharedKernel.Tenancy;

namespace Attendance.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAttendanceInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Scoped matches IMediator lifetime — do NOT register as Singleton (captive dependency bug)
        services.AddScoped<DomainEventDispatcherInterceptor>();

        services.AddDbContext<AttendanceDbContext>((sp, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("AttendanceDb"));
            options.AddInterceptors(sp.GetRequiredService<DomainEventDispatcherInterceptor>());
        });

        // Repositories registered as interfaces — DI injects ITenantContext into each,
        // satisfying AGS-007 tenant-awareness rule. AttendanceUnitOfWork receives the
        // same scoped instances via constructor injection (no `new` anti-pattern).
        services.AddScoped<IAttendanceRecordRepository,  AttendanceRecordRepository>();
        services.AddScoped<IAttendanceSummaryRepository, AttendanceSummaryRepository>();
        services.AddScoped<ICondonationRequestRepository, CondonationRequestRepository>();
        services.AddScoped<IAttendanceUnitOfWork,        AttendanceUnitOfWork>();

        return services;
    }
}
