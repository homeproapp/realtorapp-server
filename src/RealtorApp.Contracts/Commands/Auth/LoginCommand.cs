namespace RealtorApp.Contracts.Commands.Auth;

public class LoginCommand
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}
