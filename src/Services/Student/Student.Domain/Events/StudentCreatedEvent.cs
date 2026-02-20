using Student.Domain.Common;

namespace Student.Domain.Events;

public sealed class StudentCreatedEvent : DomainEvent
{
    public Guid StudentId { get; }
    public Guid TenantId { get; }
    public Guid UserId { get; }
    public string FullName { get; }

    public StudentCreatedEvent(Guid studentId, Guid tenantId, Guid userId, string fullName)
    {
        StudentId = studentId;
        TenantId = tenantId;
        UserId = userId;
        FullName = fullName;
    }
}
