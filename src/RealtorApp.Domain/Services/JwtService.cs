using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using RealtorApp.Domain.Interfaces;
using RealtorApp.Domain.Settings;

namespace RealtorApp.Domain.Services;

public class JwtService(AppSettings appSettings) : IJwtService
{
    private readonly JwtSecurityTokenHandler _tokenHandler = new();
    private readonly AppSettings _appSettings = appSettings;

    public string GenerateAccessToken(string userUuid, string role)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_appSettings.Jwt.SecretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userUuid),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("role", role, ClaimValueTypes.String)
        };

        var token = new JwtSecurityToken(
            issuer: _appSettings.Jwt.Issuer,
            audience: _appSettings.Jwt.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(_appSettings.Jwt.AccessTokenExpirationMinutes),
            signingCredentials: credentials
        );

        return _tokenHandler.WriteToken(token);
    }

}
