using Hostel.Domain.Common;
using Hostel.Domain.Enums;
using Hostel.Domain.Exceptions;
namespace Hostel.Domain.Entities;
public sealed class Hostel : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = default!;
    public HostelType Type { get; private set; }
    public int TotalRooms { get; private set; }
    public string WardenName { get; private set; } = default!;
    public string WardenContact { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    private Hostel() { }
    public static Hostel Create(Guid tenantId, string name, HostelType type, int totalRooms, string wardenName, string wardenContact)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new HostelDomainException("INVALID_NAME", "Hostel name is required.");
        if (totalRooms <= 0) throw new HostelDomainException("INVALID_ROOMS", "Total rooms must be greater than zero.");
        if (string.IsNullOrWhiteSpace(wardenName)) throw new HostelDomainException("INVALID_WARDEN", "Warden name is required.");
        if (string.IsNullOrWhiteSpace(wardenContact)) throw new HostelDomainException("INVALID_CONTACT", "Warden contact is required.");
        return new Hostel { Id = Guid.NewGuid(), TenantId = tenantId, Name = name.Trim(), Type = type,
            TotalRooms = totalRooms, WardenName = wardenName.Trim(), WardenContact = wardenContact.Trim(),
            IsActive = true, CreatedAt = DateTime.UtcNow };
    }
    public void UpdateWarden(string wardenName, string wardenContact)
    {
        if (string.IsNullOrWhiteSpace(wardenName)) throw new HostelDomainException("INVALID_WARDEN", "Warden name is required.");
        if (string.IsNullOrWhiteSpace(wardenContact)) throw new HostelDomainException("INVALID_CONTACT", "Warden contact is required.");
        WardenName = wardenName.Trim(); WardenContact = wardenContact.Trim();
    }
    public void Deactivate()
    {
        if (!IsActive) throw new HostelDomainException("ALREADY_INACTIVE", "Hostel is already inactive.");
        IsActive = false;
    }
}
