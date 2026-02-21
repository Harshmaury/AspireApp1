using Hostel.Domain.Common;
using Hostel.Domain.Enums;
using Hostel.Domain.Events;
using Hostel.Domain.Exceptions;
namespace Hostel.Domain.Entities;
public sealed class RoomAllotment : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid StudentId { get; private set; }
    public Guid RoomId { get; private set; }
    public Guid HostelId { get; private set; }
    public string AcademicYear { get; private set; } = default!;
    public int BedNumber { get; private set; }
    public AllotmentStatus Status { get; private set; }
    public DateTime AllottedAt { get; private set; }
    public DateTime? VacatedAt { get; private set; }
    private RoomAllotment() { }
    public static RoomAllotment Create(Guid tenantId, Guid studentId, Guid roomId, Guid hostelId, string academicYear, int bedNumber)
    {
        if (string.IsNullOrWhiteSpace(academicYear)) throw new HostelDomainException("INVALID_YEAR", "Academic year is required.");
        if (bedNumber <= 0) throw new HostelDomainException("INVALID_BED", "Bed number must be greater than zero.");
        var a = new RoomAllotment { Id = Guid.NewGuid(), TenantId = tenantId, StudentId = studentId,
            RoomId = roomId, HostelId = hostelId, AcademicYear = academicYear.Trim(),
            BedNumber = bedNumber, Status = AllotmentStatus.Active, AllottedAt = DateTime.UtcNow };
        a.RaiseDomainEvent(new RoomAllottedEvent(a.Id, studentId, tenantId, roomId));
        return a;
    }
    public void Vacate()
    {
        if (Status != AllotmentStatus.Active) throw new HostelDomainException("NOT_ACTIVE", "Only active allotments can be vacated.");
        Status = AllotmentStatus.Vacated; VacatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new RoomVacatedEvent(Id, StudentId, TenantId, RoomId));
    }
    public void Cancel()
    {
        if (Status != AllotmentStatus.Active) throw new HostelDomainException("NOT_ACTIVE", "Only active allotments can be cancelled.");
        Status = AllotmentStatus.Cancelled; VacatedAt = DateTime.UtcNow;
    }
}
