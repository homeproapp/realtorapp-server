using FluentValidation;
using RealtorApp.Contracts.Queries.Search.Requests;

namespace RealtorApp.Api.Validators;

public class SearchQueryValidator : AbstractValidator<SearchQuery>
{
    public SearchQueryValidator()
    {
        RuleFor(x => x.Q)
            .NotEmpty()
            .WithMessage("Search query is required")
            .MaximumLength(100)
            .WithMessage("Search query is too long");
    }
}
