using Fee.Domain.Common;
using Fee.Domain.Events;
using Fee.Domain.Exceptions;
namespace Fee.Domain.Entities;
public sealed class Scholarship : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid StudentId { get; private set; }
    public string Name { get; private set; } = default!;
    public decimal Amount { get; private set; }
    public string AcademicYear { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    private Scholarship() { }
    public static Scholarship Create(Guid tenantId, Guid studentId, string name, decimal amount, string academicYear)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new FeeDomainException("INVALID_NAME", "Scholarship name is required.");
        if (amount <= 0) throw new FeeDomainException("INVALID_AMOUNT", "Scholarship amount must be positive.");
        if (string.IsNullOrWhiteSpace(academicYear)) throw new FeeDomainException("INVALID_YEAR", "Academic year is required.");
        var scholarship = new Scholarship
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StudentId = studentId,
            Name = name.Trim(),
            Amount = amount,
            AcademicYear = academicYear.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        scholarship.RaiseDomainEvent(new ScholarshipGrantedEvent(scholarship.Id, studentId, tenantId, amount));
        return scholarship;
    }
    public void Deactivate()
    {
        if (!IsActive) throw new FeeDomainException("ALREADY_INACTIVE", "Scholarship is already inactive.");
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
    public void Activate()
    {
        if (IsActive) throw new FeeDomainException("ALREADY_ACTIVE", "Scholarship is already active.");
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
