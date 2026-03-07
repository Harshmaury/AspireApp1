// src/Services/Identity/Identity.Tests/Fakers/ApplicationUserFaker.cs
using Bogus;
using Identity.Domain.Entities;

namespace Identity.Tests.Fakers;

public static class ApplicationUserFaker
{
    private static readonly Faker F = new();

    public static ApplicationUser Active(Guid? tenantId = null)
    {
        var user = ApplicationUser.Create(
            tenantId:  tenantId ?? Guid.NewGuid(),
            email:     F.Internet.Email(),
            firstName: F.Name.FirstName(),
            lastName:  F.Name.LastName());

        user.ClearDomainEvents();
        return user;
    }

    public static ApplicationUser Unverified(Guid? tenantId = null)
    {
        var user = Active(tenantId);
        // EmailConfirmed = false by default from ASP.NET Identity
        return user;
    }

    public static ApplicationUser Verified(Guid? tenantId = null)
    {
        var user = Active(tenantId);
        user.EmailConfirmed = true;
        return user;
    }

    public static ApplicationUser Inactive(Guid? tenantId = null)
    {
        var user = Active(tenantId);
        user.Deactivate();
        user.ClearDomainEvents();
        return user;
    }
}
