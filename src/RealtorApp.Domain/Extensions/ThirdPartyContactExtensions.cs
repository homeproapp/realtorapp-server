using RealtorApp.Contracts.Commands.Contacts.Requests;
using RealtorApp.Contracts.Commands.Contacts.Responses;
using RealtorApp.Contracts.Queries.Contacts.Responses;
using RealtorApp.Infra.Data;

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

    public static ThirdPartyContactResponse[] ToResponse(this IEnumerable<ThirdPartyContact> contacts)
    {
       return [.. contacts.Select(i =>
            new ThirdPartyContactResponse()
            {
                Name = i.Name,
                Email = i.Email,
                PhoneNumber = i.Phone,
                ThirdPartyId = i.ThirdPartyContactId
            }
        )];
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
