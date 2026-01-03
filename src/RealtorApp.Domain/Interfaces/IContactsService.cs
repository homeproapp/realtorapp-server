using System;
using RealtorApp.Contracts.Commands.Contacts.Requests;
using RealtorApp.Contracts.Commands.Contacts.Responses;
using RealtorApp.Contracts.Queries.Contacts.Responses;

namespace RealtorApp.Domain.Interfaces;

public interface IContactsService
{
    Task<GetThirdPartyContactsQueryResponse> GetThirdPartyContactsAsync(long agentId);
    Task<GetThirdPartyContactQueryResponse> GetThirdPartyContactAsync(long thirdPartyContactId, long agentId);
    Task<AddOrUpdateThirdPartyContactCommandResponse> AddOrUpdateThirdPartyContactAsync(AddOrUpdateThirdPartyContactCommand command, long agentId);
    Task<DeleteThirdPartyContactCommandResponse> DeleteThirdPartyContactAsync(long thirdPartyContactId, long agentId);
    Task<GetClientContactsSlimQueryResponse> GetClientContactsSlimAsync(long agentId);
    Task<GetClientContactDetailsQueryResponse> GetClientContactDetailsAsync(long contactId, long agentId);
    Task<DeleteClientContactCommandResponse> DeleteClientContact(long contactId, long agentId);
}
