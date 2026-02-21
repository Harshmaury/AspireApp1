using MediatR;
namespace Hostel.Application.Hostels.Commands;
public sealed record UpdateWardenCommand(Guid HostelId, Guid TenantId, string WardenName, string WardenContact) : IRequest;
