namespace RealtorApp.Contracts.Commands.Auth;

public class RefreshTokenCommandResponse
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    public int ExpiresIn { get; set; }
}