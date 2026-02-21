using Identity.Domain.Entities;
using Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Identity.API.Services;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var db      = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await db.Database.MigrateAsync();

        if (await db.Tenants.AnyAsync()) return; // already seeded

        // 1 — Create default tenant
        var tenant = Tenant.Create("UMS Platform", "ums", TenantTier.Enterprise, "India");
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        // 2 — Create SuperAdmin using domain factory
        var user = ApplicationUser.Create(tenant.Id, "superadmin@ums.com", "Super", "Admin");
        var result = await userMgr.CreateAsync(user, "Admin@1234");

        if (!result.Succeeded)
            throw new Exception($"Seed failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        // 3 — Assign SuperAdmin role
        await userMgr.AddToRoleAsync(user, "SuperAdmin");

        Console.WriteLine("? Seeded: tenant=ums | superadmin@ums.com | Admin@1234");
    }
}
