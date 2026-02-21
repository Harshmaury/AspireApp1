namespace Fee.Application.DTOs;
public sealed record FeeStructureDto(Guid Id, Guid ProgrammeId, string AcademicYear, int Semester, decimal TuitionFee, decimal ExamFee, decimal DevelopmentFee, decimal MedicalFee, decimal? HostelFee, decimal? MessFee, decimal TotalFee, DateTime DueDate);
public sealed record FeePaymentDto(Guid Id, Guid StudentId, Guid FeeStructureId, decimal AmountPaid, string PaymentMode, string? TransactionId, string? Gateway, string Status, string ReceiptNumber, DateTime? PaidAt);
public sealed record ScholarshipDto(Guid Id, Guid StudentId, string Name, decimal Amount, string AcademicYear, bool IsActive);
