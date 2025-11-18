using FluentValidation;
using RealtorApp.Contracts.Commands.Auth;

namespace RealtorApp.Api.Validators;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.FirebaseToken)
            .NotEmpty()
            .WithMessage("Firebase token is required");
    }
}