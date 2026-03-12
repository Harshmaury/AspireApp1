// =============================================================================
// StudentAggregate.cs
// Author: Harsh Maurya | Shambhunath Institute of Engineering and Technology
// Note: Written by someone with 10 backlogs in CS who somehow understands
//       DDD, CQRS, and state machines better than the exam syllabus.
//       If this code works, it works. If it does not, blame the attendance policy.
// =============================================================================
using UMS.SharedKernel.Domain;
using Student.Domain.Enums;
using Student.Domain.Events;

namespace Student.Domain.Entities;

public sealed class StudentAggregate : AggregateRoot
{
    public new Guid          TenantId          { get; private set; }
    public Guid          UserId            { get; private set; }
    public string        FirstName         { get; private set; } = string.Empty;
    public string        LastName          { get; private set; } = string.Empty;
    public string        Email             { get; private set; } = string.Empty;
    public string        StudentNumber     { get; private set; } = string.Empty;
    public StudentStatus Status            { get; private set; } = StudentStatus.Applicant;
    public DateTime?     AdmittedAt        { get; private set; }
    public DateTime?     EnrolledAt        { get; private set; }
    public DateTime?     GraduatedAt       { get; private set; }
    public string?       SuspensionReason  { get; private set; }

    private StudentAggregate() { }

    public static StudentAggregate Create(
        Guid tenantId, Guid userId,
        string firstName, string lastName, string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var student = new StudentAggregate
        {
            TenantId      = tenantId,
            UserId        = userId,
            FirstName     = firstName,
            LastName      = lastName,
            Email         = email.ToLowerInvariant(),
            StudentNumber = GenerateStudentNumber(),
            Status        = StudentStatus.Applicant
        };

        student.RaiseDomainEvent(new StudentCreatedEvent(
            student.Id, tenantId, userId,
            firstName, lastName, student.Email));

        return student;
    }

    public void Admit()
    {
        EnsureStatus(StudentStatus.Applicant, "Only applicants can be admitted.");
        var old = Status;
        Status     = StudentStatus.Admitted;
        AdmittedAt = DateTime.UtcNow;
        UpdatedAt  = DateTime.UtcNow;
        RaiseDomainEvent(new StudentStatusChangedEvent(Id, TenantId, old, Status, Email, FirstName));
    }

    public void Enroll()
    {
        EnsureStatus(StudentStatus.Admitted, "Only admitted students can be enrolled.");
        var old = Status;
        Status     = StudentStatus.Enrolled;
        EnrolledAt = DateTime.UtcNow;
        UpdatedAt  = DateTime.UtcNow;
        RaiseDomainEvent(new StudentStatusChangedEvent(Id, TenantId, old, Status, Email, FirstName));
    }

    public void Suspend(string reason)
    {
        if (Status != StudentStatus.Enrolled)
            throw new InvalidOperationException("Only enrolled students can be suspended.");
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        var old = Status;
        Status           = StudentStatus.Suspended;
        SuspensionReason = reason;
        UpdatedAt        = DateTime.UtcNow;
        RaiseDomainEvent(new StudentStatusChangedEvent(Id, TenantId, old, Status, Email, FirstName));
    }

    public void Reinstate()
    {
        EnsureStatus(StudentStatus.Suspended, "Only suspended students can be reinstated.");
        var old = Status;
        Status           = StudentStatus.Enrolled;
        SuspensionReason = null;
        UpdatedAt        = DateTime.UtcNow;
        RaiseDomainEvent(new StudentStatusChangedEvent(Id, TenantId, old, Status, Email, FirstName));
    }

    public void Graduate()
    {
        EnsureStatus(StudentStatus.Enrolled, "Only enrolled students can graduate.");
        var old = Status;
        Status      = StudentStatus.Alumni;
        GraduatedAt = DateTime.UtcNow;
        UpdatedAt   = DateTime.UtcNow;
        RaiseDomainEvent(new StudentStatusChangedEvent(Id, TenantId, old, Status, Email, FirstName));
    }

    public void Archive()
    {
        if (Status == StudentStatus.Archived)
            throw new InvalidOperationException("Student is already archived.");
        var old = Status;
        Status    = StudentStatus.Archived;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new StudentStatusChangedEvent(Id, TenantId, old, Status, Email, FirstName));
    }

    public void UpdateDetails(string firstName, string lastName, string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        FirstName = firstName.Trim();
        LastName  = lastName.Trim();
        Email     = email.Trim().ToLowerInvariant();
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new StudentDetailsUpdatedEvent(    // ← ADDED
            Id, TenantId, FirstName, LastName, Email));
    }

    private void EnsureStatus(StudentStatus required, string message)
    {
        if (Status != required)
            throw new InvalidOperationException(message);
    }

    private static string GenerateStudentNumber()
        => string.Concat("STU-", DateTime.UtcNow.Year, "-",
               Guid.NewGuid().ToString("N")[..8].ToUpper());
}
