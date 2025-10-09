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

    public async Task<GetThirdPartyContactQueryResponse> GetThirdPartyContactAsync(long thirdPartyContactId)
    {
        var contact = await _context.ThirdPartyContacts.Where(i => i.ThirdPartyContactId == thirdPartyContactId)
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
            return await UpdateThirdPartyContactAsync(command);
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

    private async Task<AddOrUpdateThirdPartyContactCommandResponse> UpdateThirdPartyContactAsync(AddOrUpdateThirdPartyContactCommand command)
    {

        if (command.IsMarkedForDeletion)
        {
            await _context.ThirdPartyContacts
                .Where(i => i.ThirdPartyContactId == command.ThirdPartyId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.DeletedAt, DateTime.UtcNow));
        }
        else
        {
            await _context.ThirdPartyContacts
                .Where(c => c.ThirdPartyContactId == command.ThirdPartyId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(c => c.Name, command.Name)
                    .SetProperty(c => c.Email, command.Email)
                    .SetProperty(c => c.Phone, command.PhoneNumber)
                    .SetProperty(c => c.Trade, command.Service)
                    .SetProperty(c => c.UpdatedAt, DateTime.UtcNow));
        }

        return command.ToCommandResponse();
    }

}
