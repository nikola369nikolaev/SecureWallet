using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SecureWallet.Application.Features.Auth;
using SecureWallet.Application.Interfaces.Security;
using SecureWallet.Domain.Entities;

namespace SecureWallet.Infrastructure.Security;

public class JwtTokenService : IJwtTokenService
{
    private const int DefaultRefreshTokenExpirationDays = 7;
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateAccessToken(User user, bool securitySetupRequired = false)
    {
        string key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT key was not found.");

        string issuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("JWT issuer was not found.");

        string audience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("JWT audience was not found.");

        List<Claim> claims = new()
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(AuthClaimNames.SecuritySetupRequired, securitySetupRequired.ToString().ToLowerInvariant())
        };

        if (!string.IsNullOrWhiteSpace(user.Role?.Name))
        {
            claims.Add(new Claim(ClaimTypes.Role, user.Role.Name));
        }

        SymmetricSecurityKey signingKey = new(Encoding.UTF8.GetBytes(key));
        SigningCredentials signingCredentials = new(signingKey, SecurityAlgorithms.HmacSha256);

        SecurityTokenDescriptor tokenDescriptor = new()
        {
            Subject = new ClaimsIdentity(claims),
            Expires = GetAccessTokenExpiresAtUtc(),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = signingCredentials
        };

        JwtSecurityTokenHandler tokenHandler = new();
        SecurityToken securityToken = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(securityToken);
    }

    public DateTime GetAccessTokenExpiresAtUtc()
    {
        return DateTime.UtcNow.AddMinutes(GetAccessTokenExpirationMinutes());
    }

    public string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    public DateTime GetRefreshTokenExpiresAtUtc()
    {
        return DateTime.UtcNow.AddDays(GetRefreshTokenExpirationDays());
    }

    private int GetAccessTokenExpirationMinutes()
    {
        string expirationMinutesValue = _configuration["Jwt:AccessTokenExpirationMinutes"]
            ?? throw new InvalidOperationException("JWT access token expiration was not found.");

        if (!int.TryParse(expirationMinutesValue, out int expirationMinutes))
        {
            throw new InvalidOperationException("JWT access token expiration is invalid.");
        }

        return expirationMinutes;
    }

    private int GetRefreshTokenExpirationDays()
    {
        string? expirationDaysValue = _configuration["Jwt:RefreshTokenExpirationDays"];
        if (string.IsNullOrWhiteSpace(expirationDaysValue))
        {
            return DefaultRefreshTokenExpirationDays;
        }

        if (!int.TryParse(expirationDaysValue, out int expirationDays))
        {
            throw new InvalidOperationException("JWT refresh token expiration is invalid.");
        }

        return expirationDays;
    }
}
