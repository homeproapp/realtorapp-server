using RealtorApp.Domain.DTOs;

namespace RealtorApp.Domain.Interfaces;

public interface IAuthProviderService
{
    Task<AuthProviderUserDto?> ValidateTokenAsync(string providerToken);
}