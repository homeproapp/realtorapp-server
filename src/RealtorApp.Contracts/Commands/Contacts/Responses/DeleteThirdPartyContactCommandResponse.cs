using RealtorApp.Contracts.Common;

namespace RealtorApp.Contracts.Commands.Contacts.Responses;

public class DeleteThirdPartyContactCommandResponse : ResponseWithError
{
    public bool Success { get; set; }
}
