using Hostel.Application.DTOs;
using Hostel.Domain.Enums;
using MediatR;
namespace Hostel.Application.Hostels.Commands;
public sealed record CreateHostelCommand(Guid TenantId, string Name, HostelType Type,
    int TotalRooms, string WardenName, string WardenContact) : IRequest<HostelDto>;
