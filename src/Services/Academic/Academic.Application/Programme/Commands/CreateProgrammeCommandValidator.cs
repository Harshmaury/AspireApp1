using FluentValidation;
namespace Academic.Application.Programme.Commands;
public sealed class CreateProgrammeCommandValidator : AbstractValidator<CreateProgrammeCommand>
{
    public CreateProgrammeCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Degree).NotEmpty().MaximumLength(50);
        RuleFor(x => x.DurationYears).InclusiveBetween(1, 6);
        RuleFor(x => x.TotalCredits).GreaterThan(0);
        RuleFor(x => x.IntakeCapacity).GreaterThan(0);
    }
}