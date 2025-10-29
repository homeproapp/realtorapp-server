using FluentValidation;
using RealtorApp.Contracts.Commands.Tasks.Requests;

namespace RealtorApp.Api.Validators;

public class AddOrUpdateTaskCommandValidator : AbstractValidator<AddOrUpdateTaskCommand>
{
    public AddOrUpdateTaskCommandValidator()
    {
        RuleFor(x => x.TitleString)
            .NotEmpty()
            .WithMessage("Title is required")
            .MaximumLength(255)
            .WithMessage("Title must not exceed 255 characters");

        RuleFor(x => x.Room)
            .NotEmpty()
            .WithMessage("Room is required")
            .MaximumLength(100)
            .WithMessage("Room must not exceed 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrEmpty(x.Description))
            .WithMessage("Description must not exceed 2000 characters");

        RuleFor(x => x.Priority)
            .IsInEnum()
            .WithMessage("Invalid priority value");

        RuleForEach(x => x.Links)
            .SetValidator(new AddOrUpdateLinkRequestValidator());
    }
}

public class AddOrUpdateLinkRequestValidator : AbstractValidator<AddOrUpdateLinkRequest>
{
    public AddOrUpdateLinkRequestValidator()
    {
        RuleFor(x => x.LinkText)
            .NotEmpty()
            .MaximumLength(255)
            .When(x => !x.IsMarkedForDeletion)
            .WithMessage("Link text is required and must not exceed 255 characters");

        RuleFor(x => x.LinkUrl)
            .NotEmpty()
            .MaximumLength(500)
            .Must(BeValidUrl)
            .When(x => !x.IsMarkedForDeletion)
            .WithMessage("Valid URL is required and must not exceed 500 characters");

        RuleFor(x => x.LinkId)
            .NotNull()
            .When(x => x.IsMarkedForDeletion)
            .WithMessage("Link ID is required when marking for deletion");
    }

    private static bool BeValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}
