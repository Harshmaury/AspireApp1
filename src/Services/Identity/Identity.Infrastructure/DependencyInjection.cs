// src/Services/Identity/Identity.Infrastructure/DependencyInjection.cs
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Persistence.Repositories;
using UMS.SharedKernel.Tenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // AGS-009: Scoped - NOT singleton, no static mutable state
        services.AddScoped<UMS.SharedKernel.Tenancy.TenantContext>();
        services.AddScoped<UMS.SharedKernel.Tenancy.ITenantContext>(
            sp => sp.GetRequiredService<UMS.SharedKernel.Tenancy.TenantContext>());

        services.AddScoped<DomainEventDispatcherInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("IdentityDb"),
                npgsql => npgsql.MigrationsAssembly(
                    typeof(ApplicationDbContext).Assembly.FullName));
            options.UseOpenIddict();
            options.AddInterceptors(
                sp.GetRequiredService<DomainEventDispatcherInterceptor>());
        });

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.AllowedForNewUsers = true;
            // RequireUniqueEmail = false - uniqueness enforced per-tenant by composite index
            options.User.RequireUniqueEmail = false;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // OpenIddict Core - server config lives in API layer (Program.cs)
        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                    .UseDbContext<ApplicationDbContext>();
            });

        // Repositories
        services.AddScoped<IUserRepository,              UserRepository>();
        services.AddScoped<ITenantRepository,            TenantRepository>();
        services.AddScoped<IAuditLogger,                 AuditLogRepository>();
        services.AddScoped<IVerificationTokenRepository, VerificationTokenRepository>();

        return services;
    }
}
