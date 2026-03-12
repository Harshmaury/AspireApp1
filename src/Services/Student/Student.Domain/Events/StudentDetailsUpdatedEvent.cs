using UMS.SharedKernel.Domain;

namespace Student.Domain.Events;

public sealed class StudentDetailsUpdatedEvent : DomainEvent
{
    public Guid   StudentId       { get; }
    public override Guid TenantId { get; }
    public string FirstName       { get; }
    public string LastName        { get; }
    public string Email           { get; }

    public StudentDetailsUpdatedEvent(
        Guid studentId, Guid tenantId,
        string firstName, string lastName, string email)
    {
        StudentId = studentId;
        TenantId  = tenantId;
        FirstName = firstName;
        LastName  = lastName;
        Email     = email;
    }
}
