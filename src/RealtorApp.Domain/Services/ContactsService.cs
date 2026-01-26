using System;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using RealtorApp.Contracts.Commands.Contacts.Requests;
using RealtorApp.Contracts.Commands.Contacts.Responses;
using RealtorApp.Contracts.Queries.Contacts.Responses;
using RealtorApp.Domain.Extensions;
using RealtorApp.Domain.Interfaces;
using RealtorApp.Infra.Data;

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

    public async Task<GetThirdPartyContactsQueryResponse> BulkAddThirdPartyContactAsync(BulkAddThirdPartyContactCommand command, long agentId)
    {
        var contactsToAdd = new List<ThirdPartyContact>();
        foreach (var importedContact in command.Contacts)
        {
            var contact = new ThirdPartyContact()
            {
                Name = importedContact.Name,
                Phone = importedContact.PhoneNumber,
                Email = importedContact.Email,
                AgentId = agentId,
                Trade = string.Empty,
            };

            contactsToAdd.Add(contact);
        }

        await _context.ThirdPartyContacts.AddRangeAsync(contactsToAdd);

        await _context.SaveChangesAsync();

        return new () { ThirdPartyContacts = contactsToAdd.ToResponse() };
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
                ActiveListingsCount = i.ClientInvitationsProperties
                    .Where(i => i.PropertyInvitation.CreatedListing != null && i.PropertyInvitation.CreatedListing.DeletedAt == null).Count()
            })
            .ToArrayAsync();

        return new()
        {
            ClientContacts = contacts
        };
    }

    public async Task<DeleteClientContactCommandResponse> DeleteClientContact(long contactId, long agentId)
    {
        var clientContact = await _context.ClientInvitations
            .Where(i => i.ClientInvitationId == contactId && i.InvitedBy == agentId)
            .Select(i => new
            {
                ClientInvitation = i,
                PropertyInvitations = i.ClientInvitationsProperties.All(x => x.ClientInvitationId == contactId) ?
                    i.ClientInvitationsProperties.Select(x => x.PropertyInvitation) :
                    Array.Empty<PropertyInvitation>(),
                ClientInvitationsProperty = i.ClientInvitationsProperties.Where(i => i.ClientInvitationId == contactId),
                ActiveListings = i.ClientInvitationsProperties
                    .Where(i => i.PropertyInvitation.CreatedListing != null &&
                        i.PropertyInvitation.CreatedListing!.ClientsListings.All(x => x.ClientId == i.ClientInvitation.CreatedUserId))
                    .Select(i => new
                    {
                        Listing = i.PropertyInvitation.CreatedListing,
                        LeadAgents = i.PropertyInvitation.CreatedListing!.AgentsListings.Where(x => x.IsLeadAgent).Select(x => x.AgentId),
                        AgentListings = i.PropertyInvitation.CreatedListing!.AgentsListings,
                        ClientListings = i.PropertyInvitation.CreatedListing!.ClientsListings
                    })
            })
            .AsSplitQuery()
            .FirstOrDefaultAsync();

        if (clientContact == null)
        {
            return new() { ErrorMessage = "No contact found" };
        }

        clientContact.ClientInvitation.DeletedAt = DateTime.UtcNow;

        foreach (var propertyInvitation in clientContact.PropertyInvitations)
        {
            propertyInvitation.DeletedAt = DateTime.UtcNow;
        }

        foreach (var clientInvitationProperty in clientContact.ClientInvitationsProperty)
        {
            clientInvitationProperty.DeletedAt = DateTime.UtcNow;
        }

        foreach (var activeListing in clientContact.ActiveListings)
        {
            if (activeListing == null || activeListing.Listing == null) continue;

            // I think this is fine and I dont need to 'delete' all the child tables
            // TODO: verify this assumption
            if (activeListing.LeadAgents.Contains(agentId))
            {
                activeListing.Listing.DeletedAt = DateTime.UtcNow;
                foreach (var agentListing in activeListing.AgentListings)
                {
                    agentListing.DeletedAt = DateTime.UtcNow;
                }

                foreach (var clientListing in activeListing.ClientListings)
                {
                    clientListing.DeletedAt = DateTime.UtcNow;
                }
            }
        }

        await _context.SaveChangesAsync();

        return new() { Success = true };
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
                    .Where(i => i.DeletedAt == null && i.PropertyInvitation.DeletedAt == null)
                    .Select(x => new ClientContactListingDetailsResponse()
                    {
                        IsActive = x.PropertyInvitation.CreatedListing != null && x.PropertyInvitation.CreatedListing.AgentsListings.Any(j => j.AgentId == agentId),
                        ListingInvitationId = x.PropertyInvitationId,
                        ListingId = x.PropertyInvitation.CreatedListingId,
                        Address = x.PropertyInvitation.AddressLine1,
                        NumberOfActiveClients = x.PropertyInvitation.CreatedListingId == null ? 0 :
                            x.PropertyInvitation.CreatedListing!.ClientsListings.Count,
                        IsLeadAgent = x.PropertyInvitation.CreatedListingId == null && x.ClientInvitation.InvitedBy == agentId ||
                            x.PropertyInvitation.CreatedListing != null &&
                            x.PropertyInvitation.CreatedListing!.AgentsListings.FirstOrDefault(i => i.AgentId == agentId && i.IsLeadAgent) != null,
                        Agents = x.PropertyInvitation.CreatedListingId == null ?
                            Array.Empty<ClientContactListingAgentDetailResponse>() :
                            x.PropertyInvitation.CreatedListing!.AgentsListings.Where(y => y.AgentId != agentId)
                                .Select(y => new ClientContactListingAgentDetailResponse()
                                {
                                    Name = y.Agent.User.FirstName + ' ' + y.Agent.User.LastName.First() + '.',
                                    AgentId = y.AgentId
                                }).ToArray()
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

    public async Task<GetThirdPartyContactQueryResponse> GetThirdPartyContactByAttachmentAsync(long attachmentId)
    {
        var contact = await _context.Attachments
            .Where(a => a.AttachmentId == attachmentId && a.ContactAttachment != null)
            .AsNoTracking()
            .Select(a => new ThirdPartyContactResponse()
            {
                ThirdPartyId = a.ContactAttachment!.ThirdPartyContact.ThirdPartyContactId,
                Name = a.ContactAttachment.ThirdPartyContact.Name,
                Service = a.ContactAttachment.ThirdPartyContact.Trade,
                Email = a.ContactAttachment.ThirdPartyContact.Email,
                PhoneNumber = a.ContactAttachment.ThirdPartyContact.Phone
            })
            .FirstOrDefaultAsync();

        var response = new GetThirdPartyContactQueryResponse()
        {
            ThirdPartyContact = contact
        };

        if (contact == null)
        {
            response.ErrorMessage = "No contact attachment found";
        }

        return response;
    }
}
