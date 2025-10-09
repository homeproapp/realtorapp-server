using System;

namespace RealtorApp.Contracts.Commands.Contacts.Requests;

public class AddOrUpdateThirdPartyContactCommand
{
    public long? ThirdPartyId { get; set; }
    public string? Name { get; set; }
    public string? Service { get; set; } = string.Empty;
    public string? Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; } = string.Empty;
    public bool IsMarkedForDeletion { get; set; }
}
