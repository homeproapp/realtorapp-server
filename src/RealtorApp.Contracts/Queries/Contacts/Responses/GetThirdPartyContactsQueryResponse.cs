using System;
using RealtorApp.Contracts.Common;

namespace RealtorApp.Contracts.Queries.Contacts.Responses;

public class GetThirdPartyContactsQueryResponse : ResponseWithError
{
    public ThirdPartyContactResponse[] ThirdPartyContacts { get; set; } = [];
}

public class GetThirdPartyContactQueryResponse : ResponseWithError
{
    public ThirdPartyContactResponse? ThirdPartyContact { get; set; }
}

public class ThirdPartyContactResponse
{
    public long ThirdPartyId { get; set; }
    public string? Name { get; set; }
    public string? Service { get; set; } = string.Empty;
    public string? Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; } = string.Empty;
}
