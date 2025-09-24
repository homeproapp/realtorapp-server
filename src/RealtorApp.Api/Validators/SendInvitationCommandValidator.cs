using FluentValidation;
using RealtorApp.Contracts.Commands.Invitations;

namespace RealtorApp.Api.Validators;

public class SendInvitationCommandValidator : AbstractValidator<SendInvitationCommand>
{
    public SendInvitationCommandValidator()
    {
        RuleFor(x => x.Clients)
            .NotEmpty()
            .WithMessage("At least one client must be specified");

        RuleForEach(x => x.Clients)
            .SetValidator(new ClientInvitationRequestValidator());

        RuleFor(x => x.Clients)
            .Must(HaveUniqueEmails)
            .WithMessage("Duplicate email addresses are not allowed in the same invitation request");

        RuleFor(x => x.Properties)
            .NotEmpty()
            .WithMessage("At least one property must be specified");

        RuleForEach(x => x.Properties)
            .SetValidator(new PropertyInvitationRequestValidator());
    }

    private static bool HaveUniqueEmails(List<ClientInvitationRequest> clients)
    {
        var emails = clients.Select(c => c.Email.ToLowerInvariant()).ToList();
        return emails.Count == emails.Distinct().Count();
    }
}

public class ClientInvitationRequestValidator : AbstractValidator<ClientInvitationRequest>
{
    public ClientInvitationRequestValidator()
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

public class PropertyInvitationRequestValidator : AbstractValidator<PropertyInvitationRequest>
{
    public PropertyInvitationRequestValidator()
    {
        RuleFor(x => x.AddressLine1)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("Address line 1 is required");

        RuleFor(x => x.AddressLine2)
            .MaximumLength(200)
            .When(x => !string.IsNullOrEmpty(x.AddressLine2));

        RuleFor(x => x.City)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("City is required");

        RuleFor(x => x.Region)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Region is required");

        RuleFor(x => x.PostalCode)
            .NotEmpty()
            .MaximumLength(20)
            .WithMessage("Postal code is required");

        RuleFor(x => x.CountryCode)
            .NotEmpty()
            .Length(2)
            .WithMessage("Country code must be 2 characters");
    }
}