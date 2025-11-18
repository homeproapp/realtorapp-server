namespace RealtorApp.Domain.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(string userUuid, string role);
}