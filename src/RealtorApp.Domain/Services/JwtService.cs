using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using RealtorApp.Domain.Interfaces;
using RealtorApp.Domain.Settings;

namespace RealtorApp.Domain.Services;

public class JwtService(AppSettings appSettings) : IJwtService
{
    private readonly JsonWebTokenHandler _tokenHandler = new();
    private readonly AppSettings _appSettings = appSettings;

    public string GenerateAccessToken(Guid userUuid, string role)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_appSettings.Jwt.SecretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim("sub", userUuid.ToString()),
                new System.Security.Claims.Claim("role", role)
            }),
            Issuer = _appSettings.Jwt.Issuer,
            Audience = _appSettings.Jwt.Audience,
            Expires = DateTime.UtcNow.AddMinutes(_appSettings.Jwt.AccessTokenExpirationMinutes),
            NotBefore = DateTime.UtcNow,
            IssuedAt = DateTime.UtcNow,
            SigningCredentials = credentials,
            Claims = new Dictionary<string, object>
            {
                ["jti"] = Guid.NewGuid().ToString()
            }
        };

        return _tokenHandler.CreateToken(tokenDescriptor);
    }

}