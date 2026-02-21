using Fee.Domain.Common;
using Fee.Domain.Exceptions;
namespace Fee.Domain.Entities;
public sealed class FeeStructure : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid ProgrammeId { get; private set; }
    public string AcademicYear { get; private set; } = default!;
    public int Semester { get; private set; }
    public decimal TuitionFee { get; private set; }
    public decimal ExamFee { get; private set; }
    public decimal? HostelFee { get; private set; }
    public decimal? MessFee { get; private set; }
    public decimal DevelopmentFee { get; private set; }
    public decimal MedicalFee { get; private set; }
    public decimal TotalFee { get; private set; }
    public DateTime DueDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    private FeeStructure() { }
    public static FeeStructure Create(Guid tenantId, Guid programmeId, string academicYear, int semester, decimal tuitionFee, decimal examFee, decimal developmentFee, decimal medicalFee, DateTime dueDate, decimal? hostelFee = null, decimal? messFee = null)
    {
        if (string.IsNullOrWhiteSpace(academicYear)) throw new FeeDomainException("INVALID_YEAR", "Academic year is required.");
        if (semester < 1 || semester > 12) throw new FeeDomainException("INVALID_SEMESTER", "Invalid semester.");
        if (tuitionFee < 0) throw new FeeDomainException("INVALID_FEE", "Tuition fee cannot be negative.");
        if (examFee < 0) throw new FeeDomainException("INVALID_FEE", "Exam fee cannot be negative.");
        if (developmentFee < 0) throw new FeeDomainException("INVALID_FEE", "Development fee cannot be negative.");
        if (medicalFee < 0) throw new FeeDomainException("INVALID_FEE", "Medical fee cannot be negative.");
        var total = tuitionFee + examFee + developmentFee + medicalFee + (hostelFee ?? 0) + (messFee ?? 0);
        return new FeeStructure
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProgrammeId = programmeId,
            AcademicYear = academicYear.Trim(),
            Semester = semester,
            TuitionFee = tuitionFee,
            ExamFee = examFee,
            DevelopmentFee = developmentFee,
            MedicalFee = medicalFee,
            HostelFee = hostelFee,
            MessFee = messFee,
            TotalFee = total,
            DueDate = dueDate,
            CreatedAt = DateTime.UtcNow
        };
    }
}
