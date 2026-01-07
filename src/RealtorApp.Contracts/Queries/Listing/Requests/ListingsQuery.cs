using RealtorApp.Contracts.Common;

namespace RealtorApp.Contracts.Queries.Listing.Requests;


public class ListingsQuery
{
    public int Limit { get; set; } = 100;
    public int Offset { get; set; } = 0;
}
