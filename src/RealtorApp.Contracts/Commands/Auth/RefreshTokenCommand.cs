namespace RealtorApp.Contracts.Commands.Auth;

// TODO: Add FluentValidation validator for RefreshTokenCommand
public class RefreshTokenCommand
{
    public required string RefreshToken { get; set; }
}