// UMS - University Management System
// Key:     UMS-SHARED-P0-003-RESIDUAL
// Service: Identity
// Layer:   Domain
using UMS.SharedKernel.Domain;
using Identity.Domain.Events;
using Microsoft.AspNetCore.Identity;

namespace Identity.Domain.Entities;

public sealed class ApplicationUser : IdentityUser<Guid>, IAggregateRoot
{
    public Guid TenantId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    // Optimistic concurrency token - managed by EF, never set manually
    public byte[]? RowVersion { get; private set; }

    public Tenant? Tenant { get; private set; }

    // IAggregateRoot - cannot extend AggregateRoot (IdentityUser<Guid> is the base)
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();
    private void RaiseDomainEvent(IDomainEvent e) => _domainEvents.Add(e);

    private ApplicationUser() { }

    public static ApplicationUser Create(
        Guid tenantId, string email, string firstName, string lastName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = email.ToLowerInvariant(),
            UserName = email.ToLowerInvariant(),
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant(),
            FirstName = firstName,
            LastName = lastName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid().ToString()
        };

        user.RaiseDomainEvent(new UserRegisteredEvent(user.Id, tenantId, email));
        return user;
    }

    public void RecordLogin() => LastLoginAt = DateTime.UtcNow;
    public void Deactivate()
    {
        IsActive = false;
        RaiseDomainEvent(new UserDeactivatedEvent(Id, TenantId, Email!));
    }
    public string FullName => $"{FirstName} {LastName}";
}
