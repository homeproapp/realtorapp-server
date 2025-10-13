using FluentValidation;
using RealtorApp.Contracts.Commands.Contacts.Requests;

namespace RealtorApp.Api.Validators;

public class AddOrUpdateThirdPartyContactCommandValidator : AbstractValidator<AddOrUpdateThirdPartyContactCommand>
{
    public AddOrUpdateThirdPartyContactCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(255)
            .When(x => !x.IsMarkedForDeletion)
            .WithMessage("Name is required and must not exceed 255 characters");

        RuleFor(x => x.Email)
            .EmailAddress()
            .MaximumLength(255)
            .When(x => !string.IsNullOrEmpty(x.Email) && !x.IsMarkedForDeletion)
            .WithMessage("Valid email address is required and must not exceed 255 characters");

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20)
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber) && !x.IsMarkedForDeletion)
            .WithMessage("Phone number must not exceed 20 characters");

        RuleFor(x => x.Service)
            .MaximumLength(255)
            .When(x => !string.IsNullOrEmpty(x.Service) && !x.IsMarkedForDeletion)
            .WithMessage("Service must not exceed 255 characters");

        RuleFor(x => x.ThirdPartyId)
            .NotNull()
            .When(x => x.IsMarkedForDeletion)
            .WithMessage("Third party ID is required for updates and deletions");
    }
}
