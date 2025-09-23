namespace RealtorApp.Contracts.Commands.Auth;

// TODO: Add FluentValidation validator for LoginCommand
public class LoginCommandResponse
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    public int ExpiresIn { get; set; }
}