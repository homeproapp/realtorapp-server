using RealtorApp.Contracts.Common;

namespace RealtorApp.Contracts.Queries.Search.Responses;

public class SearchQueryResponse : ResponseWithError
{
    public SearchItemResponse[] Items { get; set; } = [];
    public bool HasMore { get; set; }
}

public class SearchItemResponse
{
    public long ListingId { get; set; }
    public string AddressLine1Templated { get; set; } = string.Empty;
    public string? AddressLine2Templated { get; set; } = string.Empty;
    public string PostalCodeTemplated { get; set; } = string.Empty;
    public string CityTemplated { get; set; } = string.Empty;
    public PersonMatchDetails[] ClientMatches { get; set; } = [];
    public PersonMatchDetails[] AgentMatches { get; set; } = [];
}

public class PersonMatchDetails
{
    public string FirstNameTemplated { get; set; } = string.Empty;
    public string LastNameTemplated  { get; set; } = string.Empty;
    public string EmailTemplated  { get; set; } = string.Empty;
    public string? PhoneTemplated { get; set; } = string.Empty;
}
