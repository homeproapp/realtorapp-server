using RealtorApp.Contracts.Common;

namespace RealtorApp.Contracts.Commands.Settings.Responses;

public class UpdateProfileCommandResponse : ResponseWithError
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
}
