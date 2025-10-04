using RealtorApp.Contracts.Common;
using RealtorApp.Contracts.Queries;

namespace RealtorApp.Contracts.Queries.User.Responses;

public class UserProfileQueryResponse : ResponseWithError
{
    public long UserId { get; set; }
    public required string Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public long? ProfileImageId { get; set; }
    public required string Role { get; set; }
}
