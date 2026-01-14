namespace RealtorApp.Domain.DTOs;

public class RawBasicSearchResultDto
{
    public required ListingSearchResult Listing { get; set; }
    public PersonSearchResult[] Agents { get; set; } = [];
    public PersonSearchResult[] Clients { get; set; } = [];
}

public class ListingSearchResult
{
    public long ListingId { get; set; }
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
}

public class PersonSearchResult
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; } = string.Empty;
}
