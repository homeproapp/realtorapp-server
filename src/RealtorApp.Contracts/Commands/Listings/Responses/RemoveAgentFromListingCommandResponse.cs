using RealtorApp.Contracts.Common;

namespace RealtorApp.Contracts.Listings.Responses;

public class RemoveAgentFromListingCommandResponse : ResponseWithError
{
    public bool Success { get; set; }
}
