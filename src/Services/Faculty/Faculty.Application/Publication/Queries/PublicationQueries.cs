using Faculty.Application.DTOs;
using MediatR;
namespace Faculty.Application.Publication.Queries;
public sealed record GetFacultyPublicationsQuery(Guid FacultyId, Guid TenantId) : IRequest<List<PublicationDto>>;
