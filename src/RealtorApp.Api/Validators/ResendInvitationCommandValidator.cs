using FluentValidation;
using RealtorApp.Contracts.Commands.Invitations;

namespace RealtorApp.Api.Validators;

public class ResendInvitationCommandValidator : AbstractValidator<ResendInvitationCommand>
{
    public ResendInvitationCommandValidator()
    {
        RuleFor(x => x.ClientInvitationId)
            .GreaterThan(0)
            .WithMessage("Valid client invitation ID is required");

        RuleFor(x => x.ClientDetails)
            .NotNull()
            .WithMessage("Client details are required")
            .SetValidator(new ClientInvitationUpdateRequestValidator());
    }
}

public class ClientInvitationUpdateRequestValidator : AbstractValidator<ClientInvitationUpdateRequest>
{
    public ClientInvitationUpdateRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .WithMessage("Valid email address is required");

        RuleFor(x => x.FirstName)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.FirstName));

        RuleFor(x => x.LastName)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.LastName));

        RuleFor(x => x.Phone)
            .MaximumLength(20)
            .When(x => !string.IsNullOrEmpty(x.Phone));
    }
}