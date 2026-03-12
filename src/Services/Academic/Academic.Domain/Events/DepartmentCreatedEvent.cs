using UMS.SharedKernel.Domain;
namespace Academic.Domain.Events;
public sealed record DepartmentCreatedEvent(Guid DepartmentId, Guid TenantId, string Name, string Code) : IDomainEvent;