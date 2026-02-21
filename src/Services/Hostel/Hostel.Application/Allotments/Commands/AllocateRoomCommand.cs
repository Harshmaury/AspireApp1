using Hostel.Application.DTOs;
using MediatR;
namespace Hostel.Application.Allotments.Commands;
public sealed record AllocateRoomCommand(Guid TenantId, Guid StudentId, Guid RoomId,
    Guid HostelId, string AcademicYear, int BedNumber) : IRequest<AllotmentDto>;
