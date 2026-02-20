using Student.Domain.Common;
using Student.Domain.Enums;

namespace Student.Domain.Events;

public sealed class StudentStatusChangedEvent : DomainEvent
{
    public Guid StudentId { get; }
    public Guid TenantId { get; }
    public StudentStatus OldStatus { get; }
    public StudentStatus NewStatus { get; }

    public StudentStatusChangedEvent(Guid studentId, Guid tenantId,
        StudentStatus oldStatus, StudentStatus newStatus)
    {
        StudentId = studentId;
        TenantId = tenantId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
    }
}
