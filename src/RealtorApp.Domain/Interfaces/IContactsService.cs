using System;
using RealtorApp.Contracts.Queries.Contacts.Responses;

namespace RealtorApp.Domain.Interfaces;

public interface IContactsService
{
    Task<GetThirdPartyContactsQueryResponse> GetThirdPartyContactsAsync(long agentId);
    Task<GetThirdPartyContactQueryResponse> GetThirdPartyContactAsync(long thirdPartyContactId);
}
