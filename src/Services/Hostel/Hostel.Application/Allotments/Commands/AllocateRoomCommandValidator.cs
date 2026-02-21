using FluentValidation;
namespace Hostel.Application.Allotments.Commands;
public sealed class AllocateRoomCommandValidator : AbstractValidator<AllocateRoomCommand>
{
    public AllocateRoomCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.RoomId).NotEmpty();
        RuleFor(x => x.HostelId).NotEmpty();
        RuleFor(x => x.AcademicYear).NotEmpty().Matches(@"^\d{4}-\d{2}$").WithMessage("Academic year must be in format YYYY-YY e.g. 2024-25");
        RuleFor(x => x.BedNumber).GreaterThan(0);
    }
}
