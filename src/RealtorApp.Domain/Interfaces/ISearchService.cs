using RealtorApp.Contracts.Queries.Search.Requests;
using RealtorApp.Contracts.Queries.Search.Responses;

namespace RealtorApp.Domain.Interfaces;

public interface ISearchService
{
    Task<SearchQueryResponse> GetEntitiesBasicSearch(SearchQuery query, long searchingUserId);
}
