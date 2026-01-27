namespace RealtorApp.Contracts.Commands.Settings.Requests;

public class UpdateProfileCommand
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? Phone { get; set; }
}
