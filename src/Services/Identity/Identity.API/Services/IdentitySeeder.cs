using Identity.Domain.Entities;
using Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Identity.API.Services;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var db      = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var cfg     = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        await db.Database.MigrateAsync();
        if (await db.Tenants.AnyAsync()) return; // already seeded

        // Read seed values from env vars (with dev fallbacks)
        var seedEmail    = cfg["UMS:SeedEmail"]      ?? "superadmin@ums.com";
        var seedPassword = cfg["UMS:SeedPassword"]   ?? "Admin@1234";
        var seedSlug     = cfg["UMS:SeedTenantSlug"] ?? "ums";

        // 1 — Create roles if they do not exist
        foreach (var role in new[] { "SuperAdmin", "Admin", "Faculty", "Student" })
        {
            if (!await roleMgr.RoleExistsAsync(role))
                await roleMgr.CreateAsync(new IdentityRole<Guid>(role));
        }

        // 2 — Create default tenant
        var tenant = Tenant.Create("UMS Platform", seedSlug, TenantTier.Enterprise, "India");
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        // 3 — Create SuperAdmin user
        var user   = ApplicationUser.Create(tenant.Id, seedEmail, "Super", "Admin");
        var result = await userMgr.CreateAsync(user, seedPassword);
        if (!result.Succeeded)
            throw new Exception($"Seed failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        // 4 — Assign role
        await userMgr.AddToRoleAsync(user, "SuperAdmin");
        Console.WriteLine($"? Seeded: tenant={seedSlug} | {seedEmail}");
    }
}
