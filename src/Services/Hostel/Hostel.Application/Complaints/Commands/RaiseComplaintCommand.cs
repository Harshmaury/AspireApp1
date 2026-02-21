using Hostel.Application.DTOs;
using Hostel.Domain.Enums;
using MediatR;
namespace Hostel.Application.Complaints.Commands;
public sealed record RaiseComplaintCommand(Guid TenantId, Guid StudentId, Guid HostelId,
    ComplaintCategory Category, string Description) : IRequest<ComplaintDto>;
