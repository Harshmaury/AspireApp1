using FluentValidation;
namespace Academic.Application.AcademicCalendar.Commands;
public sealed class CreateAcademicCalendarCommandValidator : AbstractValidator<CreateAcademicCalendarCommand>
{
    public CreateAcademicCalendarCommandValidator()
    {
        RuleFor(x => x.AcademicYear).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Semester).InclusiveBetween(1, 2);
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate).WithMessage("End date must be after start date.");
        RuleFor(x => x.ExamEndDate).GreaterThan(x => x.ExamStartDate).WithMessage("Exam end date must be after exam start date.");
    }
}