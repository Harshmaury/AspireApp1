using Academic.Domain.Common;
namespace Academic.Domain.Events;
public sealed record CoursePublishedEvent(Guid CourseId, Guid TenantId, string Code, string Name) : IDomainEvent;