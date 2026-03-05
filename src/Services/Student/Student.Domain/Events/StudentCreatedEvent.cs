using Student.Domain.Common;

namespace Student.Domain.Events;

public sealed class StudentCreatedEvent : DomainEvent
{
    public Guid   StudentId       { get; }
    public override Guid TenantId { get; }
    public Guid   UserId          { get; }
    public string FirstName       { get; }
    public string LastName        { get; }
    public string Email           { get; }

    public StudentCreatedEvent(
        Guid studentId, Guid tenantId, Guid userId,
        string firstName, string lastName, string email)
    {
        StudentId = studentId;
        TenantId  = tenantId;
        UserId    = userId;
        FirstName = firstName;
        LastName  = lastName;
        Email     = email;
    }
}
