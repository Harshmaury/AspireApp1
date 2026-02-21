using FluentValidation;
namespace Attendance.Application.Condonation.Commands;
public sealed class CreateCondonationRequestCommandValidator : AbstractValidator<CreateCondonationRequestCommand>
{
    public CreateCondonationRequestCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.DocumentUrl).MaximumLength(500).When(x => x.DocumentUrl is not null);
    }
}
public sealed class ApproveCondonationCommandValidator : AbstractValidator<ApproveCondonationCommand>
{
    public ApproveCondonationCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.RequestId).NotEmpty();
        RuleFor(x => x.ReviewedBy).NotEmpty();
        RuleFor(x => x.Note).MaximumLength(500).When(x => x.Note is not null);
    }
}
public sealed class RejectCondonationCommandValidator : AbstractValidator<RejectCondonationCommand>
{
    public RejectCondonationCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.RequestId).NotEmpty();
        RuleFor(x => x.ReviewedBy).NotEmpty();
        RuleFor(x => x.Note).NotEmpty().MaximumLength(500);
    }
}
