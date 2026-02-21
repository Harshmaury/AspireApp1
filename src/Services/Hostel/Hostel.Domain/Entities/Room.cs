using Hostel.Domain.Common;
using Hostel.Domain.Enums;
using Hostel.Domain.Exceptions;
namespace Hostel.Domain.Entities;
public sealed class Room : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid HostelId { get; private set; }
    public string RoomNumber { get; private set; } = default!;
    public int Floor { get; private set; }
    public RoomType Type { get; private set; }
    public int Capacity { get; private set; }
    public int CurrentOccupancy { get; private set; }
    public RoomStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    private Room() { }
    public static Room Create(Guid tenantId, Guid hostelId, string roomNumber, int floor, RoomType type, int capacity)
    {
        if (string.IsNullOrWhiteSpace(roomNumber)) throw new HostelDomainException("INVALID_ROOM_NUMBER", "Room number is required.");
        if (capacity <= 0 || capacity > 4) throw new HostelDomainException("INVALID_CAPACITY", "Room capacity must be between 1 and 4.");
        if (floor < 0) throw new HostelDomainException("INVALID_FLOOR", "Floor cannot be negative.");
        return new Room { Id = Guid.NewGuid(), TenantId = tenantId, HostelId = hostelId,
            RoomNumber = roomNumber.Trim().ToUpper(), Floor = floor, Type = type,
            Capacity = capacity, CurrentOccupancy = 0, Status = RoomStatus.Available, CreatedAt = DateTime.UtcNow };
    }
    public void IncrementOccupancy()
    {
        if (CurrentOccupancy >= Capacity) throw new HostelDomainException("ROOM_FULL", "Room is already at full capacity.");
        CurrentOccupancy++;
        if (CurrentOccupancy == Capacity) Status = RoomStatus.FullyOccupied;
    }
    public void DecrementOccupancy()
    {
        if (CurrentOccupancy <= 0) throw new HostelDomainException("INVALID_OCCUPANCY", "Occupancy cannot go below zero.");
        CurrentOccupancy--;
        if (Status == RoomStatus.FullyOccupied) Status = RoomStatus.Available;
    }
    public void SetMaintenance()
    {
        if (CurrentOccupancy > 0) throw new HostelDomainException("ROOM_OCCUPIED", "Cannot set room under maintenance while occupied.");
        Status = RoomStatus.UnderMaintenance;
    }
    public void ClearMaintenance()
    {
        if (Status != RoomStatus.UnderMaintenance) throw new HostelDomainException("NOT_IN_MAINTENANCE", "Room is not under maintenance.");
        Status = RoomStatus.Available;
    }
}
