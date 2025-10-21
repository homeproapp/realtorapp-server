using System;
using Microsoft.EntityFrameworkCore;
using RealtorApp.Contracts.Commands.Contacts.Requests;
using RealtorApp.Contracts.Commands.Contacts.Responses;
using RealtorApp.Contracts.Queries.Contacts.Responses;
using RealtorApp.Domain.Extensions;
using RealtorApp.Domain.Interfaces;
using RealtorApp.Domain.Models;

namespace RealtorApp.Domain.Services;

public class ContactsService(RealtorAppDbContext context) : IContactsService
{
    private readonly RealtorAppDbContext _context = context;

    public async Task<GetThirdPartyContactsQueryResponse> GetThirdPartyContactsAsync(long agentId)
    {
        var contacts = await _context.ThirdPartyContacts.Where(i => i.AgentId == agentId)
            .AsNoTracking()
            .Select(i => new ThirdPartyContactResponse()
            {
                ThirdPartyId = i.ThirdPartyContactId,
                Name = i.Name,
                Service = i.Trade,
                Email = i.Email,
                PhoneNumber = i.Phone
            }).ToArrayAsync();

        return new GetThirdPartyContactsQueryResponse()
        {
            ThirdPartyContacts = contacts
        };
    }

    public async Task<GetThirdPartyContactQueryResponse> GetThirdPartyContactAsync(long thirdPartyContactId, long agentId)
    {
        var contact = await _context.ThirdPartyContacts
            .Where(i => i.ThirdPartyContactId == thirdPartyContactId && i.AgentId == agentId)
            .AsNoTracking()
            .Select(i => new ThirdPartyContactResponse()
            {
                ThirdPartyId = i.ThirdPartyContactId,
                Name = i.Name,
                Service = i.Trade,
                Email = i.Email,
                PhoneNumber = i.Phone
            }).FirstOrDefaultAsync();

        var response = new GetThirdPartyContactQueryResponse()
        {
            ThirdPartyContact = contact
        };

        if (contact == null)
        {
            response.ErrorMessage = "No third party contact found";
        }

        return response;
    }

    public async Task<AddOrUpdateThirdPartyContactCommandResponse> AddOrUpdateThirdPartyContactAsync(AddOrUpdateThirdPartyContactCommand command, long agentId)
    {
        if (command.ThirdPartyId == null)
        {
            return await AddThirdPartyContactAsync(command, agentId);
        }
        else
        {
            return await UpdateThirdPartyContactAsync(command, agentId);
        }
    }

    private async Task<AddOrUpdateThirdPartyContactCommandResponse> AddThirdPartyContactAsync(AddOrUpdateThirdPartyContactCommand command, long agentId)
    {
        var contact = new ThirdPartyContact()
        {
            Name = command.Name,
            Phone = command.PhoneNumber,
            Email = command.Email,
            AgentId = agentId,
            Trade = command.Service
        };

        await _context.ThirdPartyContacts.AddAsync(contact);

        await _context.SaveChangesAsync();

        return contact.ToCommandResponse();
    }

    private async Task<AddOrUpdateThirdPartyContactCommandResponse> UpdateThirdPartyContactAsync(AddOrUpdateThirdPartyContactCommand command, long agentId)
    {

        if (command.IsMarkedForDeletion)
        {
            await _context.ThirdPartyContacts
                .Where(i => i.ThirdPartyContactId == command.ThirdPartyId && i.AgentId == agentId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.DeletedAt, DateTime.UtcNow));
        }
        else
        {
            await _context.ThirdPartyContacts
                .Where(c => c.ThirdPartyContactId == command.ThirdPartyId && c.AgentId == agentId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(c => c.Name, command.Name)
                    .SetProperty(c => c.Email, command.Email)
                    .SetProperty(c => c.Phone, command.PhoneNumber)
                    .SetProperty(c => c.Trade, command.Service)
                    .SetProperty(c => c.UpdatedAt, DateTime.UtcNow));
        }

        return command.ToCommandResponse();
    }

    public async Task<DeleteThirdPartyContactCommandResponse> DeleteThirdPartyContactAsync(long thirdPartyContactId, long agentId)
    {
        await _context.ThirdPartyContacts
            .Where(c => c.ThirdPartyContactId == thirdPartyContactId && c.AgentId == agentId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(c => c.DeletedAt, DateTime.UtcNow));

        return new DeleteThirdPartyContactCommandResponse
        {
            Success = true
        };
    }

    public async Task<GetClientContactsSlimQueryResponse> GetClientContactsSlimAsync(long agentId)
    {
        var contacts = await _context.ClientInvitations
            .AsNoTracking()
            .Where(i => i.InvitedBy == agentId)
            .Select(i => new ClientContactSlimResponse()
            {
                ContactId = i.ClientInvitationId,
                FirstName = i.CreatedUser == null ? i.ClientFirstName : i.CreatedUser.User.FirstName,
                LastName = i.CreatedUser == null ? i.ClientLastName : i.CreatedUser.User.LastName,
                Email = i.CreatedUser == null ? i.ClientEmail : i.CreatedUser.User.Email,
                Phone = i.ClientPhone,
                HasAcceptedInvite = i.AcceptedAt != null,
                InviteHasExpired = i.ExpiresAt < DateTime.UtcNow,
                ActiveListingsCount = i.CreatedUser == null ? 0 :
                    i.CreatedUser.ClientsListings
                        .Select(x => x.Listing.AgentsListings)
                        .Where(i => i.Any(i => i.AgentId == agentId))
                        .Count()
            })
            .ToArrayAsync();

        return new()
        {
            ClientContacts = contacts
        };
    }

    public async Task<GetClientContactDetailsQueryResponse> GetClientContactDetailsAsync(long contactId, long agentId)
    {
        var client = await _context.ClientInvitations
            .AsNoTracking()
            .Where(i => i.InvitedBy == agentId && i.ClientInvitationId == contactId)
            .Select(i => new ClientContactDetailsResponse()
            {
                ContactId = i.ClientInvitationId,
                FirstName = i.CreatedUser == null ? i.ClientFirstName : i.CreatedUser.User.FirstName,
                LastName = i.CreatedUser == null ? i.ClientLastName : i.CreatedUser.User.LastName,
                Email = i.CreatedUser == null ? i.ClientEmail : i.CreatedUser.User.Email,
                Phone = i.CreatedUser == null ? i.ClientPhone : i.CreatedUser.User.Phone,
                HasAcceptedInvite = i.AcceptedAt != null,
                InviteHasExpired = i.AcceptedAt == null && i.ExpiresAt < DateTime.UtcNow,
                AssociatedWith = i.ClientInvitationsProperties
                    .SelectMany(x => x.PropertyInvitation.ClientInvitationsProperties)
                    .Where(i => i.ClientInvitationId != contactId)
                    .Select(cip => new ClientContactAssociatedUsers()
                    {
                        ContactId = cip.ClientInvitationId,
                        FirstName = cip.ClientInvitation.CreatedUser == null ? cip.ClientInvitation.ClientFirstName :
                            cip.ClientInvitation.CreatedUser.User.FirstName,
                        LastName = cip.ClientInvitation.CreatedUser == null ? cip.ClientInvitation.ClientLastName :
                            cip.ClientInvitation.CreatedUser.User.LastName,
                        Email = cip.ClientInvitation.CreatedUser == null ? cip.ClientInvitation.ClientEmail :
                            cip.ClientInvitation.CreatedUser.User.Email,
                        Phone = cip.ClientInvitation.CreatedUser == null ? cip.ClientInvitation.ClientPhone :
                            cip.ClientInvitation.CreatedUser.User.Phone,
                        HasAcceptedInvite = cip.ClientInvitation.AcceptedAt != null,
                        InviteHasExpired = cip.ClientInvitation.ExpiresAt < DateTime.UtcNow,
                    })
                    .ToArray(),
                Listings = i.ClientInvitationsProperties
                    .Select(x => new ClientContactListingDetailsResponse()
                    {
                        IsActive = x.PropertyInvitation.CreatedListing != null && x.PropertyInvitation.CreatedListing.AgentsListings.Any(j => j.AgentId == agentId),
                        ListingInvitationId = x.PropertyInvitationId,
                        ListingId = x.PropertyInvitation.CreatedListingId,
                        Address = x.PropertyInvitation.AddressLine1,
                        Agents = x.PropertyInvitation.CreatedListing == null ?
                            Array.Empty<ClientContactListingAgentDetailResponse>() :
                            x.PropertyInvitation.CreatedListing.AgentsListings.Where(y => y.AgentId != agentId)
                                .Select(y => new ClientContactListingAgentDetailResponse()
                                {
                                    Name = y.Agent.User.FirstName + ' ' + y.Agent.User.LastName.First() + '.',
                                    AgentId = y.AgentId
                                }).ToArray(),
                    }).ToArray()
            })
            .FirstOrDefaultAsync();

        if (client == null)
        {
            return new()
            {
                ErrorMessage = "No client found"
            };
        }

        //remove duplicates
        client.AssociatedWith = [.. client.AssociatedWith.DistinctBy(x => x.ContactId)];
        return new()
        {
            Contact = client
        };
    }

}
