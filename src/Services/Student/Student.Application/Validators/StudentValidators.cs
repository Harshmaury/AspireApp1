using FluentValidation;
using Student.Application.Features.Students.Commands;

namespace Student.Application.Validators;

public sealed class CreateStudentCommandValidator : AbstractValidator<CreateStudentCommand>
{
    public CreateStudentCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("TenantId is required.");
        RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId is required.");
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100)
            .WithMessage("First name is required and must be 100 characters or fewer.");
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100)
            .WithMessage("Last name is required and must be 100 characters or fewer.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256)
            .WithMessage("A valid email address is required.");
    }
}

public sealed class UpdateStudentCommandValidator : AbstractValidator<UpdateStudentCommand>
{
    public UpdateStudentCommandValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty().WithMessage("StudentId is required.");
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("TenantId is required.");
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100)
            .WithMessage("First name is required and must be 100 characters or fewer.");
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100)
            .WithMessage("Last name is required and must be 100 characters or fewer.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256)
            .WithMessage("A valid email address is required.");
    }
}

public sealed class SuspendStudentCommandValidator : AbstractValidator<SuspendStudentCommand>
{
    public SuspendStudentCommandValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500)
            .WithMessage("Suspension reason is required and must be 500 characters or fewer.");
    }
}
