// src/Services/Identity/Identity.Application/Features/Auth/Commands/VerifyEmailCommandValidator.cs
using FluentValidation;

namespace Identity.Application.Features.Auth.Commands;

public sealed class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
{
    public VerifyEmailCommandValidator()
    {
        RuleFor(x => x.RawToken)
            .NotEmpty().WithMessage("Token is required.")
            .MinimumLength(10).WithMessage("Token is invalid.")
            .MaximumLength(200).WithMessage("Token is invalid.");
    }
}
