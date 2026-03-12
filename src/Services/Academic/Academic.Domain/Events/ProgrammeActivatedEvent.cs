using UMS.SharedKernel.Domain;
namespace Academic.Domain.Events;
public sealed record ProgrammeActivatedEvent(Guid ProgrammeId, Guid TenantId, string Code) : IDomainEvent;