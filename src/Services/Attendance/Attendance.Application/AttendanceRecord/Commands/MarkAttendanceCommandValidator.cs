using FluentValidation;
namespace Attendance.Application.AttendanceRecord.Commands;
public sealed class MarkAttendanceCommandValidator : AbstractValidator<MarkAttendanceCommand>
{
    public MarkAttendanceCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.AcademicYear).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Semester).InclusiveBetween(1, 12);
        RuleFor(x => x.Date).NotEmpty()
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Cannot mark attendance for a future date.")
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)))
            .WithMessage("Cannot backdate attendance more than 7 days.");
        RuleFor(x => x.ClassType).NotEmpty()
            .Must(v => new[] { "Lecture", "Tutorial", "Lab" }.Contains(v, StringComparer.OrdinalIgnoreCase))
            .WithMessage("ClassType must be Lecture, Tutorial, or Lab.");
        RuleFor(x => x.MarkedBy).NotEmpty();
    }
}
