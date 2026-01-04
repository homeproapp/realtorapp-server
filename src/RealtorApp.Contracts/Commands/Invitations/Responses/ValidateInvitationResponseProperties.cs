namespace RealtorApp.Contracts.Commands.Invitations.Responses;

public class ValidateInvitationResponseProperties
{
    public string AddressLine1 {get; set;} = string.Empty;
    public string AddressLine2 {get; set;} = string.Empty;
    public string City {get; set;} = string.Empty;
    public string PostalCode {get; set;} = string.Empty;
    public string Region {get; set;} = string.Empty;
    public string CountryCode {get; set;} = string.Empty;
}
