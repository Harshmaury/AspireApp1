using FluentValidation;
namespace Academic.Application.Course.Commands;
public sealed class CreateCourseCommandValidator : AbstractValidator<CreateCourseCommand>
{
    private static readonly string[] ValidCourseTypes = ["Core", "Elective", "Lab", "Audit", "MOOC"];
    public CreateCourseCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Credits).InclusiveBetween(1, 6);
        RuleFor(x => x.CourseType).NotEmpty().Must(t => ValidCourseTypes.Contains(t)).WithMessage("CourseType must be one of: Core, Elective, Lab, Audit, MOOC.");
        RuleFor(x => x.LectureHours).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TutorialHours).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PracticalHours).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxEnrollment).GreaterThan(0);
    }
}