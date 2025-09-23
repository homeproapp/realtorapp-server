namespace RealtorApp.Domain.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(Guid userUuid, string role);
}