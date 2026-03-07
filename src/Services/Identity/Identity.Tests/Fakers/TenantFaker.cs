// src/Services/Identity/Identity.Tests/Fakers/TenantFaker.cs
using Bogus;
using Identity.Domain.Entities;

namespace Identity.Tests.Fakers;

public static class TenantFaker
{
    private static readonly Faker F = new();

    public static Tenant Active(
        TenantTier tier   = TenantTier.Shared,
        string?    region = null,
        bool selfReg      = true)
    {
        var t = Tenant.Create(
            name:   F.Company.CompanyName(),
            slug:   F.Internet.DomainWord().ToLowerInvariant(),
            tier:   tier,
            region: region ?? "default");

        t.Activate();

        if (selfReg)
            t.UpdateFeatures(TenantFeatures.Default().WithSelfRegistration(true));

        t.ClearDomainEvents();
        return t;
    }

    public static Tenant Suspended()
    {
        var t = Active();
        t.Suspend();
        t.ClearDomainEvents();
        return t;
    }

    public static Tenant Cancelled()
    {
        var t = Active();
        t.Cancel();
        t.ClearDomainEvents();
        return t;
    }

    public static Tenant WithNoSelfRegistration()
        => Active(selfReg: false);

    public static Tenant AtUserLimit(TenantTier tier = TenantTier.Shared)
    {
        // MaxUsers for Shared = 100; we set Features but can't set MaxUsers directly,
        // so this returns a tenant whose CanAddUsers(MaxUsers) == false.
        return Active(tier: tier);
    }
}
