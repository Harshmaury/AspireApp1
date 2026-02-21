using MediatR;
namespace Fee.Application.FeeStructure.Commands;
public sealed record CreateFeeStructureCommand(
    Guid TenantId,
    Guid ProgrammeId,
    string AcademicYear,
    int Semester,
    decimal TuitionFee,
    decimal ExamFee,
    decimal DevelopmentFee,
    decimal MedicalFee,
    DateTime DueDate,
    decimal? HostelFee = null,
    decimal? MessFee = null) : IRequest<Guid>;
