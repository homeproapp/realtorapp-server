namespace RealtorApp.Domain.DTOs;

public class UserRefreshTokenDto
{
    public long UserId { get; set; }
    public Guid UserUuid { get; set; }
    public string Role { get; set; } = string.Empty;
}