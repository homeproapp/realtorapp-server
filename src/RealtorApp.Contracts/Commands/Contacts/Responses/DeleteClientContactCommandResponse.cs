using RealtorApp.Contracts.Common;

namespace RealtorApp.Contracts.Commands.Contacts.Responses;

public class DeleteClientContactCommandResponse : ResponseWithError
{
    public bool Success { get; set; }
}
