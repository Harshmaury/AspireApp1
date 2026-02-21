using MediatR;
using Faculty.Application.Interfaces;
using Faculty.Domain.Enums;
using Faculty.Domain.Exceptions;
using PublicationEntity = Faculty.Domain.Entities.Publication;
namespace Faculty.Application.Publication.Commands;
public sealed class AddPublicationCommandHandler : IRequestHandler<AddPublicationCommand, Guid>
{
    private readonly IPublicationRepository _repository;
    public AddPublicationCommandHandler(IPublicationRepository repository) => _repository = repository;
    public async Task<Guid> Handle(AddPublicationCommand cmd, CancellationToken ct)
    {
        var type = Enum.Parse<PublicationType>(cmd.Type, true);
        var pub = PublicationEntity.Create(cmd.TenantId, cmd.FacultyId, cmd.Title, cmd.Journal, cmd.PublishedYear, type, cmd.DOI);
        await _repository.AddAsync(pub, ct);
        return pub.Id;
    }
}
public sealed class UpdateCitationCountCommandHandler : IRequestHandler<UpdateCitationCountCommand>
{
    private readonly IPublicationRepository _repository;
    public UpdateCitationCountCommandHandler(IPublicationRepository repository) => _repository = repository;
    public async Task Handle(UpdateCitationCountCommand cmd, CancellationToken ct)
    {
        var pub = await _repository.GetByIdAsync(cmd.PublicationId, cmd.TenantId, ct)
            ?? throw new FacultyDomainException("NOT_FOUND", $"Publication {cmd.PublicationId} not found.");
        pub.UpdateCitations(cmd.CitationCount);
        await _repository.UpdateAsync(pub, ct);
    }
}
