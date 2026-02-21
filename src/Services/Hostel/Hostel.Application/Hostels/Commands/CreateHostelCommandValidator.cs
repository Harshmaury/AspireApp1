using FluentValidation;
namespace Hostel.Application.Hostels.Commands;
public sealed class CreateHostelCommandValidator : AbstractValidator<CreateHostelCommand>
{
    public CreateHostelCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TotalRooms).GreaterThan(0);
        RuleFor(x => x.WardenName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.WardenContact).NotEmpty().MaximumLength(20);
    }
}
