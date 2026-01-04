namespace RealtorApp.Contracts.Commands.Invitations.Requests;

public class PropertyInvitationRequest
{
    public required string AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public required string City { get; set; }
    public required string Region { get; set; }
    public required string PostalCode { get; set; }
    public required string CountryCode { get; set; }
}
