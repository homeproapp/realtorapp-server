using RealtorApp.Domain.DTOs;

namespace RealtorApp.Domain.Interfaces;

public interface IRefreshTokenService
{
    string GenerateRefreshToken();
    string HashRefreshToken(string refreshToken);
    bool VerifyRefreshTokenHash(string refreshToken, string storedHash);
    Task<string> CreateRefreshTokenAsync(long userId);
    Task<bool> ValidateRefreshTokenAsync(string refreshToken);
    Task<UserRefreshTokenDto?> GetUserByRefreshTokenAsync(string refreshToken);
    Task RevokeRefreshTokenAsync(string refreshToken);
    Task RevokeAllUserRefreshTokensAsync(long userId);
}