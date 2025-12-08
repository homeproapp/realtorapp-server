using System;
using Microsoft.EntityFrameworkCore;
using RealtorApp.Contracts.Queries.Listing.Responses;
using RealtorApp.Contracts.Queries.Responses;
using RealtorApp.Domain.Interfaces;
using RealtorApp.Infra.Data;

namespace RealtorApp.Domain.Services;

public class ListingService(RealtorAppDbContext context) : IListingService
{
    private readonly RealtorAppDbContext _context = context;

    public async Task<ListingDetailsSlimQueryResponse> GetListingDetailsSlim(long listingId)
    {
        return await _context.Listings
            .Where(l => l.ListingId == listingId)
            .Select(l => new ListingDetailsSlimQueryResponse()
            {
                ClientNames = l.ClientsListings.Select(cl => cl.Client.User.FirstName + " " + cl.Client.User.LastName).ToArray(),
                AgentNames = l.AgentsListings.Select(al => al.Agent.User.FirstName + " " + al.Agent.User.LastName).ToArray(),
                Address = l.Property.AddressLine1,
                OtherListings = l.ClientsListings
                    .SelectMany(cl => cl.Client.ClientsListings)
                    .Where(cll => cll.ListingId != listingId)
                    .Select(cll => new OtherListing
                    {
                        ListingId = cll.ListingId,
                        Address = cll.Listing.Property.AddressLine1
                    })
                    .Distinct()
                    .ToArray()
            }).FirstAsync();
    }

    public async Task<ActiveListingsQueryResponse> GetAgentActiveListings(long agentId)
    {
        var activeListings = await _context.AgentsListings
            .Where(i => i.AgentId == agentId)
            .Select(i => new ActiveListing()
            {
                ListingId = i.ListingId,
                AddressLine1 = i.Listing.Property.AddressLine1,
                City = i.Listing.Property.City,
                Region = i.Listing.Property.Region
            }).ToArrayAsync();

        return new() { ActiveListings = activeListings };
    }

    public async Task<ActiveListingsQueryResponse> GetClientActiveListings(long clientId)
    {
        var activeListings = await _context.ClientsListings
            .Where(i => i.ClientId == clientId)
            .Select(i => new ActiveListing()
            {
                ListingId = i.ListingId,
                AddressLine1 = i.Listing.Property.AddressLine1,
                City = i.Listing.Property.City,
                Region = i.Listing.Property.Region
            }).ToArrayAsync();

        return new() { ActiveListings = activeListings };
    }
}
