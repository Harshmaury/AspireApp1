using Hostel.Domain.Enums;
namespace Hostel.Application.DTOs;

public sealed record HostelDto(Guid Id, Guid TenantId, string Name, HostelType Type,
    int TotalRooms, string WardenName, string WardenContact, bool IsActive, DateTime CreatedAt);

public sealed record RoomDto(Guid Id, Guid HostelId, string RoomNumber, int Floor,
    RoomType Type, int Capacity, int CurrentOccupancy, RoomStatus Status);

public sealed record AllotmentDto(Guid Id, Guid StudentId, Guid RoomId, Guid HostelId,
    string AcademicYear, int BedNumber, AllotmentStatus Status, DateTime AllottedAt, DateTime? VacatedAt);

public sealed record ComplaintDto(Guid Id, Guid StudentId, Guid HostelId,
    ComplaintCategory Category, string Description, ComplaintStatus Status,
    string? ResolutionNote, DateTime CreatedAt, DateTime? ResolvedAt);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int PageNumber, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
