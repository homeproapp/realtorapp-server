using RealtorApp.Contracts.Commands.Contacts.Requests;
using RealtorApp.Contracts.Commands.Contacts.Responses;
using RealtorApp.Domain.Models;

namespace RealtorApp.Domain.Extensions;

public static class ThirdPartyContactExtensions
{
    public static AddOrUpdateThirdPartyContactCommandResponse ToCommandResponse(this ThirdPartyContact contact)
    {
        return new AddOrUpdateThirdPartyContactCommandResponse
        {
            ThirdPartyId = contact.ThirdPartyContactId,
            Name = contact.Name,
            Service = contact.Trade,
            Email = contact.Email,
            PhoneNumber = contact.Phone
        };
    }

    public static AddOrUpdateThirdPartyContactCommandResponse ToCommandResponse(this AddOrUpdateThirdPartyContactCommand command)
    {
        return new AddOrUpdateThirdPartyContactCommandResponse
        {
            ThirdPartyId = command.ThirdPartyId ?? 0,
            Name = command.Name,
            Service = command.Service,
            Email = command.Email,
            PhoneNumber = command.PhoneNumber
        };
    }
}
