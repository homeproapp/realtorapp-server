namespace RealtorApp.Contracts.Commands.Auth;

// TODO: Add FluentValidation validator for LogoutCommand
public class LogoutCommand
{
    public required string RefreshToken { get; set; }
}