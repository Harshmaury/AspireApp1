using FluentValidation;
namespace Academic.Application.Department.Commands;
public sealed class CreateDepartmentCommandValidator : AbstractValidator<CreateDepartmentCommand>
{
    public CreateDepartmentCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(10);
        RuleFor(x => x.EstablishedYear).InclusiveBetween(1800, DateTime.UtcNow.Year);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
    }
}