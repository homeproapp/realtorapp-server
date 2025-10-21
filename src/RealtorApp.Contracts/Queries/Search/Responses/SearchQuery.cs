using RealtorApp.Contracts.Common;

namespace RealtorApp.Contracts.Queries.Search.Responses;

public class SearchQueryResponse : ResponseWithError
{
    
}

public class SearchItemResponse
{
    public required string ListingTextHtml { get; set; }
    public required string ClientsTextHtml { get; set; }
    public long ListingId { get; set; }
}