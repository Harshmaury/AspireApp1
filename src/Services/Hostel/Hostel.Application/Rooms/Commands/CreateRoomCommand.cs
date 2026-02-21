using Hostel.Application.DTOs;
using Hostel.Domain.Enums;
using MediatR;
namespace Hostel.Application.Rooms.Commands;
public sealed record CreateRoomCommand(Guid TenantId, Guid HostelId, string RoomNumber,
    int Floor, RoomType Type, int Capacity) : IRequest<RoomDto>;
