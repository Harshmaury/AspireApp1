using FluentValidation;
namespace Academic.Application.Curriculum.Commands;
public sealed class AddCourseToCurriculumCommandValidator : AbstractValidator<AddCourseToCurriculumCommand>
{
    public AddCourseToCurriculumCommandValidator()
    {
        RuleFor(x => x.Semester).InclusiveBetween(1, 12);
        RuleFor(x => x.Version).NotEmpty().MaximumLength(10);
    }
}