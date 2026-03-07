// src/Services/Identity/Identity.Application/Features/Auth/Commands/ForgotPasswordCommandValidator.cs
using FluentValidation;

namespace Identity.Application.Features.Auth.Commands;

public sealed class ForgotPasswordCommandValidator
    : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.TenantSlug)
            .NotEmpty().WithMessage("Tenant slug is required.")
            .MaximumLength(100)
            .Matches("^[a-z0-9-]+$")
            .WithMessage("Slug must be lowercase alphanumeric with hyphens only.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(256);
    }
}
