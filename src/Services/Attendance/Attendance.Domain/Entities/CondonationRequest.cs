using Attendance.Domain.Common;
using Attendance.Domain.Enums;
using Attendance.Domain.Events;
using Attendance.Domain.Exceptions;
namespace Attendance.Domain.Entities;
public sealed class CondonationRequest : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid StudentId { get; private set; }
    public Guid CourseId { get; private set; }
    public string Reason { get; private set; } = default!;
    public string? DocumentUrl { get; private set; }
    public CondonationStatus Status { get; private set; }
    public Guid? ReviewedBy { get; private set; }
    public string? ReviewNote { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    private CondonationRequest() { }
    public static CondonationRequest Create(Guid tenantId, Guid studentId, Guid courseId, string reason, string? documentUrl = null)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new AttendanceDomainException("INVALID_REASON", "Reason is required.");
        if (reason.Length > 1000) throw new AttendanceDomainException("REASON_TOO_LONG", "Reason cannot exceed 1000 characters.");
        return new CondonationRequest
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StudentId = studentId,
            CourseId = courseId,
            Reason = reason.Trim(),
            DocumentUrl = documentUrl?.Trim(),
            Status = CondonationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }
    public void Approve(Guid reviewedBy, string? note = null)
    {
        if (Status != CondonationStatus.Pending) throw new AttendanceDomainException("INVALID_STATUS", "Only pending requests can be approved.");
        Status = CondonationStatus.Approved;
        ReviewedBy = reviewedBy;
        ReviewNote = note?.Trim();
        ReviewedAt = DateTime.UtcNow;
        RaiseDomainEvent(new CondonationApprovedEvent(Id, StudentId, TenantId, CourseId));
    }
    public void Reject(Guid reviewedBy, string note)
    {
        if (Status != CondonationStatus.Pending) throw new AttendanceDomainException("INVALID_STATUS", "Only pending requests can be rejected.");
        if (string.IsNullOrWhiteSpace(note)) throw new AttendanceDomainException("REVIEW_NOTE_REQUIRED", "Rejection note is required.");
        Status = CondonationStatus.Rejected;
        ReviewedBy = reviewedBy;
        ReviewNote = note.Trim();
        ReviewedAt = DateTime.UtcNow;
    }
}
