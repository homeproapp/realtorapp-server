using Microsoft.EntityFrameworkCore;
using RealtorApp.Contracts.Queries.Search.Requests;
using RealtorApp.Contracts.Queries.Search.Responses;
using RealtorApp.Domain.DTOs;
using RealtorApp.Domain.Extensions;
using RealtorApp.Domain.Helpers;
using RealtorApp.Domain.Interfaces;
using RealtorApp.Infra.Data;
using Task = System.Threading.Tasks.Task;

namespace RealtorApp.Domain.Services;

public class SearchService(RealtorAppDbContext dbContext) : ISearchService
{
    private readonly RealtorAppDbContext _dbContext = dbContext;

    public async Task<SearchQueryResponse> GetEntitiesBasicSearch(SearchQuery query, long searchingUserId)
    {
        var searchPattern = $"%{query.Q}%";

        var matches = await _dbContext.AgentsListings
            .Where(i => i.AgentId == searchingUserId &&
                (EF.Functions.ILike(i.Listing.Property.AddressLine1, searchPattern) ||
                EF.Functions.ILike(i.Listing.Property.AddressLine2 ?? string.Empty, searchPattern) ||
                EF.Functions.ILike(i.Listing.Property.City, searchPattern) ||
                EF.Functions.ILike(i.Listing.Property.PostalCode, searchPattern) ||
                i.Listing.AgentsListings.Any(x => EF.Functions.ILike(x.Agent.User.FirstName, searchPattern) || EF.Functions.ILike(x.Agent.User.LastName, searchPattern)) ||
                i.Listing.ClientsListings.Any(x => EF.Functions.ILike(x.Client.User.FirstName, searchPattern) || EF.Functions.ILike(x.Client.User.LastName, searchPattern))))
            .Skip(query.Offset)
            .Take(query.Limit + 1)
            .AsNoTracking()
            .Select(i => new RawBasicSearchResultDto()
            {
                Listing = new()
                {
                    ListingId = i.ListingId,
                    AddressLine1 = i.Listing.Property.AddressLine1,
                    AddressLine2 = i.Listing.Property.AddressLine2,
                    City = i.Listing.Property.City,
                    PostalCode = i.Listing.Property.PostalCode,
                },
                Agents = i.Listing.AgentsListings
                    .Where(x => x.AgentId != searchingUserId)
                    .Select(x => new PersonSearchResult()
                    {
                        FirstName = x.Agent.User.FirstName,
                        LastName = x.Agent.User.LastName,
                        Email = x.Agent.User.Email,
                        Phone = x.Agent.User.Phone
                    })
                    .ToArray(),
                Clients = i.Listing.ClientsListings
                    .Select(x => new PersonSearchResult()
                    {
                        FirstName = x.Client.User.FirstName,
                        LastName = x.Client.User.LastName,
                        Email = x.Client.User.Email,
                        Phone = x.Client.User.Phone
                    })
                    .ToArray()
            })
            .ToArrayAsync();

        var hasMore = matches.Length > query.Limit;
        var results = hasMore ? matches[..query.Limit] : matches;

        var regex = SearchResultTemplateHelper.CreateSearchTermRegex(query.Q);
        var items = results.ToSearchItemResponses(regex);

        return new SearchQueryResponse
        {
            Items = items,
            HasMore = hasMore
        };
    }
}
