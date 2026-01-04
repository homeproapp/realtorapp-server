using FluentValidation;
using RealtorApp.Contracts.Commands.Invitations.Requests;

namespace RealtorApp.Api.Validators;

public class AcceptInvitationCommandValidator : AbstractValidator<AcceptInvitationCommand>
{
    public AcceptInvitationCommandValidator()
    {
        RuleFor(x => x.InvitationToken)
            .NotEmpty()
            .WithMessage("Invitation token is required");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required")
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
