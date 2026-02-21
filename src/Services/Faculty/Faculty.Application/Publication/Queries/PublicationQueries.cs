using MediatR;
using Faculty.Application.DTOs;
namespace Faculty.Application.Publication.Queries;
public sealed record GetFacultyPublicationsQuery(Guid FacultyId, Guid TenantId) : IRequest<List<PublicationDto>>;
