using MediatR;
using Attendance.Application.Interfaces;
using CondonationEntity = Attendance.Domain.Entities.CondonationRequest;
namespace Attendance.Application.Condonation.Commands;
public sealed class CreateCondonationRequestCommandHandler : IRequestHandler<CreateCondonationRequestCommand, Guid>
{
    private readonly ICondonationRequestRepository _repository;
    public CreateCondonationRequestCommandHandler(ICondonationRequestRepository repository) => _repository = repository;
    public async Task<Guid> Handle(CreateCondonationRequestCommand cmd, CancellationToken ct)
    {
        var request = CondonationEntity.Create(cmd.TenantId, cmd.StudentId, cmd.CourseId, cmd.Reason, cmd.DocumentUrl);
        await _repository.AddAsync(request, ct);
        return request.Id;
    }
}
public sealed class ApproveCondonationCommandHandler : IRequestHandler<ApproveCondonationCommand>
{
    private readonly ICondonationRequestRepository _repository;
    public ApproveCondonationCommandHandler(ICondonationRequestRepository repository) => _repository = repository;
    public async Task Handle(ApproveCondonationCommand cmd, CancellationToken ct)
    {
        var request = await _repository.GetByIdAsync(cmd.RequestId, cmd.TenantId, ct) ?? throw new Attendance.Domain.Exceptions.AttendanceDomainException("NOT_FOUND", $"Condonation request {cmd.RequestId} not found.");
        request.Approve(cmd.ReviewedBy, cmd.Note);
        await _repository.UpdateAsync(request, ct);
    }
}
public sealed class RejectCondonationCommandHandler : IRequestHandler<RejectCondonationCommand>
{
    private readonly ICondonationRequestRepository _repository;
    public RejectCondonationCommandHandler(ICondonationRequestRepository repository) => _repository = repository;
    public async Task Handle(RejectCondonationCommand cmd, CancellationToken ct)
    {
        var request = await _repository.GetByIdAsync(cmd.RequestId, cmd.TenantId, ct) ?? throw new Attendance.Domain.Exceptions.AttendanceDomainException("NOT_FOUND", $"Condonation request {cmd.RequestId} not found.");
        request.Reject(cmd.ReviewedBy, cmd.Note);
        await _repository.UpdateAsync(request, ct);
    }
}

