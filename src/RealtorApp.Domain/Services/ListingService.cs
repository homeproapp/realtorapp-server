using System;
using Microsoft.EntityFrameworkCore;
using RealtorApp.Contracts.Listings.Responses;
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

    public async Task<ListingsQueryResponse> GetAllListingsForAgent(long agentId)
    {
        var listings = await _context.PropertyInvitations.Where(i => i.InvitedBy == agentId && i.DeletedAt == null)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new ListingItem
            {
                ListingId = i.CreatedListingId,
                AddressLine1 = i.AddressLine1,
                AddressLine2 = i.AddressLine2,
                ListingAgents = i.CreatedListingId == null ? Array.Empty<ListingAgent>() :
                    i.CreatedListing!.AgentsListings
                        .Where(x => x.AgentId != agentId)
                        .Select(x => new ListingAgent() { Name = x.Agent.User.FirstName + " " + x.Agent.User.LastName })
                        .ToArray(),
                InvitedTeammates = i.InvitedByNavigation.TeammateInvitations
                    .Where(x => x.InvitedListingId == i.CreatedListingId && x.CreatedUserId == null)
                    .Select(x => new PendingTeammateInvitation() { TeammateInvitationId = x.TeammateInvitationId, Email = x.TeammateEmail, IsExpired = x.ExpiresAt < DateTime.UtcNow })
                    .ToArray(),
                Status = i.CreatedListingId != null && i.CreatedListing!.DeletedAt == null ? "Active" :
                    i.ClientInvitationsProperties.All(x => x.ClientInvitation.ExpiresAt < DateTime.UtcNow) ? "Invite Expired" :
                    "Pending"
            })
            .AsNoTracking()
            .ToArrayAsync();

        return new()
        {
            Listings = listings,
            HasMore = false // I dont suspect we'll actually need this pagination..
        };
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

    public async Task<DeleteListingCommandResponse> DeleteListing(long listingId, long agentId)
    {
        var isLeadAgent = await _context.AgentsListings
            .FirstOrDefaultAsync(i => i.AgentId == agentId && i.ListingId == listingId && i.IsLeadAgent);

        if (isLeadAgent == null)
        {
            return new() { ErrorMessage = "Not lead" };
        }

        await _context.Listings.Where(i => i.ListingId == listingId)
            .ExecuteUpdateAsync(setter => setter.SetProperty(i => i.DeletedAt, DateTime.UtcNow));

        await _context.AgentsListings.Where(i => i.ListingId == listingId)
            .ExecuteUpdateAsync(setter => setter.SetProperty(i => i.DeletedAt, DateTime.UtcNow));

        await _context.ClientsListings.Where(i => i.ListingId == listingId)
            .ExecuteUpdateAsync(setter => setter.SetProperty(i => i.DeletedAt, DateTime.UtcNow));

        await _context.PropertyInvitations.Where(i => i.CreatedListingId == listingId)
            .ExecuteUpdateAsync(setter => setter.SetProperty(i => i.DeletedAt, DateTime.UtcNow));

        await _context.ClientInvitationsProperties.Where(i => i.PropertyInvitation.CreatedListingId == listingId)
            .ExecuteUpdateAsync(setter => setter.SetProperty(i => i.DeletedAt, DateTime.UtcNow));

        return new() { Success = true };
    }
}
