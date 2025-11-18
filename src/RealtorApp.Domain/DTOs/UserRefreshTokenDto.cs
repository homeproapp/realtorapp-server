namespace RealtorApp.Domain.DTOs;

public class UserRefreshTokenDto
{
    public long UserId { get; set; }
    public required string UserUuid { get; set; }
    public string Role { get; set; } = string.Empty;
}