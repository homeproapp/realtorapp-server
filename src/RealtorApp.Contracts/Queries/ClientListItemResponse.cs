using System;

namespace RealtorApp.Contracts.Queries;

public class ClientListItemResponse
{
    public long ClientId { get; set; }

    public required string FirstName { get; set; }

    public required string LastName { get; set; }
    
    public long? ProfileImageId { get; set; }

}
