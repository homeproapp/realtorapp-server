using FluentValidation;
using RealtorApp.Contracts.Commands.Auth;

namespace RealtorApp.Api.Validators;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.FirebaseToken)
            .NotEmpty()
            .Must(BeValidGuid)
            .WithMessage("Invalid request");
    }

    private static bool BeValidGuid(string token)
    {
        return Guid.TryParse(token, out _);
    }
}