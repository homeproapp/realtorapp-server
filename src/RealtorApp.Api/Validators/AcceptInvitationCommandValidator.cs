using FluentValidation;
using RealtorApp.Contracts.Commands.Invitations;

namespace RealtorApp.Api.Validators;

public class AcceptInvitationCommandValidator : AbstractValidator<AcceptInvitationCommand>
{
    public AcceptInvitationCommandValidator()
    {
        RuleFor(x => x.InvitationToken)
            .NotEmpty()
            .WithMessage("Invitation token is required");

        RuleFor(x => x.FirebaseToken)
            .NotEmpty()
            .WithMessage("Firebase token is required");
    }
}