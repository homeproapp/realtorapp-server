using FluentValidation;
using RealtorApp.Contracts.Commands.Settings.Requests;

namespace RealtorApp.Api.Validators;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty()
            .WithMessage("Current password is required");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("New password is required")
            .MinimumLength(10)
            .WithMessage("Password must be at least 10 characters")
            .Matches(@"[!@#$%^&*(),.?"":{}|<>]")
            .WithMessage("Password must contain at least one special character")
            .Matches(@"[A-Z]")
            .WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"[a-z]")
            .WithMessage("Password must contain at least one lowercase letter")
            .Matches(@"[0-9]")
            .WithMessage("Password must contain at least one number");
    }
}
