using System.Text.RegularExpressions;
using RealtorApp.Contracts.Queries.Search.Responses;
using RealtorApp.Domain.DTOs;
using RealtorApp.Domain.Helpers;

namespace RealtorApp.Domain.Extensions;

public static class SearchExtensions
{
    public static SearchItemResponse[] ToSearchItemResponses(this RawBasicSearchResultDto[] results, Regex searchTermRegex)
    {
        return results.Select(r => new SearchItemResponse
        {
            ListingId = r.Listing.ListingId,
            AddressLine1Templated = SearchResultTemplateHelper.AddTagsAroundSearchTermMatch(r.Listing.AddressLine1, searchTermRegex),
            AddressLine2Templated = SearchResultTemplateHelper.AddTagsAroundSearchTermMatch(r.Listing.AddressLine2 ?? string.Empty, searchTermRegex),
            PostalCodeTemplated = SearchResultTemplateHelper.AddTagsAroundSearchTermMatch(r.Listing.PostalCode, searchTermRegex),
            CityTemplated = SearchResultTemplateHelper.AddTagsAroundSearchTermMatch(r.Listing.City, searchTermRegex),
            AgentMatches = r.Agents.Select(a => new PersonMatchDetails
            {
                FirstNameTemplated = SearchResultTemplateHelper.AddTagsAroundSearchTermMatch(a.FirstName, searchTermRegex),
                LastNameTemplated = SearchResultTemplateHelper.AddTagsAroundSearchTermMatch(a.LastName, searchTermRegex),
                EmailTemplated = SearchResultTemplateHelper.AddTagsAroundSearchTermMatch(a.Email, searchTermRegex),
                PhoneTemplated = SearchResultTemplateHelper.AddTagsAroundSearchTermMatch(a.Phone ?? string.Empty, searchTermRegex)
            }).ToArray(),
            ClientMatches = r.Clients.Select(c => new PersonMatchDetails
            {
                FirstNameTemplated = SearchResultTemplateHelper.AddTagsAroundSearchTermMatch(c.FirstName, searchTermRegex),
                LastNameTemplated = SearchResultTemplateHelper.AddTagsAroundSearchTermMatch(c.LastName, searchTermRegex),
                EmailTemplated = SearchResultTemplateHelper.AddTagsAroundSearchTermMatch(c.Email, searchTermRegex),
                PhoneTemplated = SearchResultTemplateHelper.AddTagsAroundSearchTermMatch(c.Phone ?? string.Empty, searchTermRegex)
            }).ToArray()
        }).ToArray();
    }
}
