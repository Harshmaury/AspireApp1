using Hostel.Domain.Common;
using Hostel.Domain.Enums;
using Hostel.Domain.Events;
using Hostel.Domain.Exceptions;
namespace Hostel.Domain.Entities;
public sealed class HostelComplaint : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid StudentId { get; private set; }
    public Guid HostelId { get; private set; }
    public ComplaintCategory Category { get; private set; }
    public string Description { get; private set; } = default!;
    public ComplaintStatus Status { get; private set; }
    public string? ResolutionNote { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    private HostelComplaint() { }
    public static HostelComplaint Create(Guid tenantId, Guid studentId, Guid hostelId, ComplaintCategory category, string description)
    {
        if (string.IsNullOrWhiteSpace(description)) throw new HostelDomainException("INVALID_DESCRIPTION", "Complaint description is required.");
        if (description.Length > 1000) throw new HostelDomainException("DESCRIPTION_TOO_LONG", "Description cannot exceed 1000 characters.");
        var c = new HostelComplaint { Id = Guid.NewGuid(), TenantId = tenantId, StudentId = studentId,
            HostelId = hostelId, Category = category, Description = description.Trim(),
            Status = ComplaintStatus.Open, CreatedAt = DateTime.UtcNow };
        c.RaiseDomainEvent(new ComplaintRaisedEvent(c.Id, studentId, tenantId, category.ToString()));
        return c;
    }
    public void MarkInProgress()
    {
        if (Status != ComplaintStatus.Open) throw new HostelDomainException("INVALID_STATUS", "Only open complaints can be marked in-progress.");
        Status = ComplaintStatus.InProgress;
    }
    public void Resolve(string resolutionNote)
    {
        if (Status == ComplaintStatus.Resolved || Status == ComplaintStatus.Closed)
            throw new HostelDomainException("ALREADY_RESOLVED", "Complaint is already resolved.");
        if (string.IsNullOrWhiteSpace(resolutionNote)) throw new HostelDomainException("NOTE_REQUIRED", "Resolution note is required.");
        Status = ComplaintStatus.Resolved; ResolutionNote = resolutionNote.Trim();
        ResolvedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ComplaintResolvedEvent(Id, TenantId));
    }
    public void Close()
    {
        if (Status != ComplaintStatus.Resolved) throw new HostelDomainException("NOT_RESOLVED", "Only resolved complaints can be closed.");
        Status = ComplaintStatus.Closed;
    }
}
