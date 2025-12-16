using FirebaseAdmin.Auth;
using RealtorApp.Domain.DTOs;

namespace RealtorApp.Domain.Interfaces;

public interface IAuthProviderService
{
    Task<AuthProviderUserDto?> ValidateTokenAsync(string providerToken);
    Task<UserRecord?> RegisterWithEmailAndPasswordAsync(string email, string password, bool emailVerified);
    Task<AuthProviderUserDto?> SignInWithEmailAndPasswordAsync(string email, string password);
}