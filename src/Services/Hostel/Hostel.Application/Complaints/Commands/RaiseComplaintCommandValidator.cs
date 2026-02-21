using FluentValidation;
namespace Hostel.Application.Complaints.Commands;
public sealed class RaiseComplaintCommandValidator : AbstractValidator<RaiseComplaintCommand>
{
    public RaiseComplaintCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.HostelId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
    }
}
