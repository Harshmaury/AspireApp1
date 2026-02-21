using Examination.Domain.Common;
using Examination.Domain.Exceptions;
namespace Examination.Domain.Entities;
public sealed class HallTicket : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid StudentId { get; private set; }
    public Guid ExamScheduleId { get; private set; }
    public string RollNumber { get; private set; } = default!;
    public string SeatNumber { get; private set; } = default!;
    public bool IsEligible { get; private set; }
    public string? IneligibilityReason { get; private set; }
    public DateTime IssuedAt { get; private set; }
    private HallTicket() { }
    public static HallTicket Create(Guid tenantId, Guid studentId, Guid examScheduleId, string rollNumber, string seatNumber, bool isEligible, string? ineligibilityReason = null)
    {
        if (string.IsNullOrWhiteSpace(rollNumber)) throw new ExaminationDomainException("INVALID_ROLL", "Roll number is required.");
        if (string.IsNullOrWhiteSpace(seatNumber)) throw new ExaminationDomainException("INVALID_SEAT", "Seat number is required.");
        if (!isEligible && string.IsNullOrWhiteSpace(ineligibilityReason)) throw new ExaminationDomainException("INELIGIBILITY_REASON_REQUIRED", "Ineligibility reason is required when student is not eligible.");
        return new HallTicket
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StudentId = studentId,
            ExamScheduleId = examScheduleId,
            RollNumber = rollNumber.Trim(),
            SeatNumber = seatNumber.Trim(),
            IsEligible = isEligible,
            IneligibilityReason = ineligibilityReason?.Trim(),
            IssuedAt = DateTime.UtcNow
        };
    }
}
