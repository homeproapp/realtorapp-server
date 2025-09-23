namespace RealtorApp.Contracts.Commands.Auth;

public class LoginCommand
{
    public required string FirebaseToken { get; set; }
}