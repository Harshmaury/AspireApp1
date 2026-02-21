using MediatR;
using Examination.Application.Interfaces;
namespace Examination.Application.MarksEntry.Commands;
public sealed class SubmitMarksCommandHandler : IRequestHandler<SubmitMarksCommand>
{
    private readonly IMarksEntryRepository _repository;
    public SubmitMarksCommandHandler(IMarksEntryRepository repository) => _repository = repository;
    public async Task Handle(SubmitMarksCommand cmd, CancellationToken ct)
    {
        var entry = await _repository.GetByIdAsync(cmd.MarksEntryId, cmd.TenantId, ct) ?? throw new Exception("Marks entry not found.");
        entry.Submit();
        await _repository.UpdateAsync(entry, ct);
    }
}
public sealed class ApproveMarksCommandHandler : IRequestHandler<ApproveMarksCommand>
{
    private readonly IMarksEntryRepository _repository;
    public ApproveMarksCommandHandler(IMarksEntryRepository repository) => _repository = repository;
    public async Task Handle(ApproveMarksCommand cmd, CancellationToken ct)
    {
        var entry = await _repository.GetByIdAsync(cmd.MarksEntryId, cmd.TenantId, ct) ?? throw new Exception("Marks entry not found.");
        entry.Approve(cmd.ApprovedBy);
        await _repository.UpdateAsync(entry, ct);
    }
}
public sealed class PublishMarksCommandHandler : IRequestHandler<PublishMarksCommand>
{
    private readonly IMarksEntryRepository _repository;
    public PublishMarksCommandHandler(IMarksEntryRepository repository) => _repository = repository;
    public async Task Handle(PublishMarksCommand cmd, CancellationToken ct)
    {
        var entry = await _repository.GetByIdAsync(cmd.MarksEntryId, cmd.TenantId, ct) ?? throw new Exception("Marks entry not found.");
        entry.Publish();
        await _repository.UpdateAsync(entry, ct);
    }
}
