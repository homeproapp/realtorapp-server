namespace RealtorApp.Domain.DTOs;

public class AuthProviderUserDto
{
    public required string Uid { get; set; }
    public required string Email { get; set; }
    public string? DisplayName { get; set; }
}