using System;
using RealtorApp.Contracts.Common;

namespace RealtorApp.Contracts.Queries.Contacts.Responses;

public class GetClientContactsSlimQueryResponse : ResponseWithError
{
    public ClientContactSlimResponse[] ClientContacts { get; set; } = [];
}

public class ClientContactSlimResponse
{
    public long ContactId { get; set; }
    public long? ClientId { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public bool HasAcceptedInvite { get; set; }
    public bool InviteHasExpired { get; set; }
    public int ActiveListingsCount { get; set; }
}