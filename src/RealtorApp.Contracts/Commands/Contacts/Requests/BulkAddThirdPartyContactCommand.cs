using System;

namespace RealtorApp.Contracts.Commands.Contacts.Requests;

public class BulkAddThirdPartyContactCommand
{
    public ImportThirdPartyContact[] Contacts { get; set; } = [];
}

public class ImportThirdPartyContact
{
    public required string Name { get; set; }
    public string? Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; } = string.Empty;
}
