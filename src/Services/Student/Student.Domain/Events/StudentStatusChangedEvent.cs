using Student.Domain.Common;
using Student.Domain.Enums;

namespace Student.Domain.Events;

public sealed class StudentStatusChangedEvent : DomainEvent
{
    public Guid          StudentId  { get; }
    public override Guid TenantId   { get; }
    public StudentStatus OldStatus  { get; }
    public StudentStatus NewStatus  { get; }
    public string        Email      { get; }
    public string        FirstName  { get; }

    public StudentStatusChangedEvent(
        Guid studentId, Guid tenantId,
        StudentStatus oldStatus, StudentStatus newStatus,
        string email, string firstName)
    {
        StudentId = studentId;
        TenantId  = tenantId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        Email     = email;
        FirstName = firstName;
    }
}
