using MediatR;
using Faculty.Application.DTOs;
using Faculty.Application.Interfaces;
using PublicationEntity = Faculty.Domain.Entities.Publication;
namespace Faculty.Application.Publication.Queries;
internal static class PublicationMapper
{
    internal static PublicationDto ToDto(PublicationEntity p)
        => new(p.Id, p.FacultyId, p.Title, p.Journal, p.PublishedYear, p.Type.ToString(), p.DOI, p.CitationCount);
}
public sealed class GetFacultyPublicationsQueryHandler : IRequestHandler<GetFacultyPublicationsQuery, List<PublicationDto>>
{
    private readonly IPublicationRepository _repository;
    public GetFacultyPublicationsQueryHandler(IPublicationRepository repository) => _repository = repository;
    public async Task<List<PublicationDto>> Handle(GetFacultyPublicationsQuery query, CancellationToken ct)
    {
        var list = await _repository.GetByFacultyAsync(query.FacultyId, query.TenantId, ct);
        return list.Select(PublicationMapper.ToDto).ToList();
    }
}
