using RealtorApp.Contracts.Common;

namespace RealtorApp.Contracts.Listings.Responses;

public class DeleteListingCommandResponse : ResponseWithError
{
    public bool Success { get; set; }
}
