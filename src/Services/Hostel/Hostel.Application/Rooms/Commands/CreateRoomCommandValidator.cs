using FluentValidation;
namespace Hostel.Application.Rooms.Commands;
public sealed class CreateRoomCommandValidator : AbstractValidator<CreateRoomCommand>
{
    public CreateRoomCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.HostelId).NotEmpty();
        RuleFor(x => x.RoomNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Floor).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Capacity).InclusiveBetween(1, 4);
    }
}
