using Academic.Domain.Common;
namespace Academic.Domain.Events;
public sealed record ProgrammeActivatedEvent(Guid ProgrammeId, Guid TenantId, string Code) : IDomainEvent;