using FirebaseAdmin.Auth;
using RealtorApp.Domain.DTOs;

namespace RealtorApp.Domain.Interfaces;

public interface IAuthProviderService
{
    Task<AuthProviderUserDto?> ValidateTokenAsync(string providerToken);
    Task<UserRecord?> RegisterWithEmailAndPasswordAsync(string email, string password, bool emailVerified);
    Task<AuthProviderUserDto?> SignInWithEmailAndPasswordAsync(string email, string password);
    Task<bool> DeleteUserAsync(string uid);
    Task<bool> UpdateEmailAsync(string uid, string newEmail);
    Task<bool> ChangePasswordAsync(string uid, string currentPassword, string newPassword);
}