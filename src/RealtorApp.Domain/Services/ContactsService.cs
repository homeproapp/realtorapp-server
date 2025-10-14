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

    public async Task<GetClientContactsSlimQueryResponse> GetClientContactsAsync(long agentId)
    {
        var contacts = await _context.ClientInvitations
            .AsNoTracking()
            .Where(i => i.InvitedBy == agentId)
            .Select(i => new ClientContactSlimResponse()
            {
                ContactId = i.ClientInvitationId,
                FirstName = i.ClientFirstName,
                LastName = i.ClientLastName,
                Email = i.ClientEmail,
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

}
