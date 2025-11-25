namespace RealtorApp.Contracts.Commands.Auth;

public class LoginCommand
{
    public required string FirebaseToken { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsClient { get; set; } = false;
}