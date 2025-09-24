using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RealtorApp.Domain.DTOs;
using RealtorApp.Domain.Interfaces;
using RealtorAppDbContext = RealtorApp.Domain.Models.RealtorAppDbContext;
using RefreshToken = RealtorApp.Domain.Models.RefreshToken;
using RealtorApp.Domain.Settings;

namespace RealtorApp.Domain.Services;

public class RefreshTokenService(RealtorAppDbContext context, AppSettings appSettings) : IRefreshTokenService
{
    private readonly RealtorAppDbContext _context = context;
    private readonly AppSettings _appSettings = appSettings;

    public string GenerateRefreshToken()
    {
        byte[] tokenBytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(tokenBytes);
    }

    public string HashRefreshToken(string refreshToken)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
    }

    public bool VerifyRefreshTokenHash(string refreshToken, string storedHash)
    {
        string incomingHash = HashRefreshToken(refreshToken);
        return storedHash.Equals(incomingHash, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<string> CreateRefreshTokenAsync(long userId)
    {
        var refreshToken = GenerateRefreshToken();
        var tokenHash = HashRefreshToken(refreshToken);

        var refreshTokenEntity = new RefreshToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(30), // TODO: move to settings
            CreatedAt = DateTime.UtcNow
        };

        _context.RefreshTokens.Add(refreshTokenEntity);
        await Task.CompletedTask;
        // await _context.SaveChangesAsync();

        return refreshToken;
    }

    public async Task<bool> ValidateRefreshTokenAsync(string refreshToken)
    {
        var tokenHash = HashRefreshToken(refreshToken);

        var storedToken = await _context.RefreshTokens
            .Where(rt => rt.TokenHash == tokenHash &&
                         rt.RevokedAt == null &&
                         rt.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync();

        return storedToken != null;
    }

    public async Task<UserRefreshTokenDto?> GetUserByRefreshTokenAsync(string refreshToken)
    {
        var tokenHash = HashRefreshToken(refreshToken);

        var result = await _context.RefreshTokens
            .Include(rt => rt.User)
                .ThenInclude(u => u.Agent)
            .Include(rt => rt.User)
                .ThenInclude(u => u.Client)
            .Where(rt => rt.TokenHash == tokenHash &&
                         rt.RevokedAt == null &&
                         rt.ExpiresAt > DateTime.UtcNow)
            .Select(rt => new UserRefreshTokenDto
            {
                UserId = rt.User.UserId,
                UserUuid = rt.User.Uuid ?? Guid.Empty,
                Role = rt.User.Agent != null ? "agent" : "client"
            })
            .FirstOrDefaultAsync();

        return result;
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken)
    {
        var tokenHash = HashRefreshToken(refreshToken);

        var storedToken = await _context.RefreshTokens
            .Where(rt => rt.TokenHash == tokenHash && rt.RevokedAt == null)
            .FirstOrDefaultAsync();

        if (storedToken != null)
        {
            storedToken.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task RevokeAllUserRefreshTokensAsync(long userId)
    {
        await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ExecuteUpdateAsync(rt => rt.SetProperty(r => r.RevokedAt, DateTime.UtcNow));
    }
}