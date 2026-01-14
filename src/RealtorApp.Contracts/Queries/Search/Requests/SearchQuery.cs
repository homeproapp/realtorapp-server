namespace RealtorApp.Contracts.Queries.Search.Requests;

public class SearchQuery
{
    public required string Q { get; set; }
    public int Limit { get; set; } = 25;
    public int Offset { get; set; } = 0;
}
