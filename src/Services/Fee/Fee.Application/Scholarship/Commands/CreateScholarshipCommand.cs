using MediatR;
namespace Fee.Application.Scholarship.Commands;
public sealed record CreateScholarshipCommand(Guid TenantId, Guid StudentId, string Name, decimal Amount, string AcademicYear) : IRequest<Guid>;
