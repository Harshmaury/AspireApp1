using Faculty.Domain.Enums;
using MediatR;
namespace Faculty.Application.Publication.Commands;
public sealed record AddPublicationCommand(Guid TenantId, Guid FacultyId, string Title, string Journal, int PublishedYear, string Type, string? DOI = null) : IRequest<Guid>;
public sealed record UpdateCitationCountCommand(Guid TenantId, Guid PublicationId, int CitationCount) : IRequest;
