using FluentValidation;
namespace Hostel.Application.Hostels.Commands;
public sealed class UpdateWardenCommandValidator : AbstractValidator<UpdateWardenCommand>
{
    public UpdateWardenCommandValidator()
    {
        RuleFor(x => x.HostelId).NotEmpty();
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.WardenName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.WardenContact).NotEmpty().MaximumLength(20);
    }
}
