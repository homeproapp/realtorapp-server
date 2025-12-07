using RealtorApp.Infra.Data;

namespace RealtorApp.Domain.Extensions;

public static class PropertyInvitationExtensions
{
    public static Property ToProperty(this PropertyInvitation propertyInvitation)
    {
        return new Property
        {
            AddressLine1 = propertyInvitation.AddressLine1,
            AddressLine2 = propertyInvitation.AddressLine2,
            City = propertyInvitation.City,
            Region = propertyInvitation.Region,
            PostalCode = propertyInvitation.PostalCode,
            CountryCode = propertyInvitation.CountryCode
        };
    }
}
