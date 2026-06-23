using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SecureWallet.Application.Features.Auth;
using SecureWallet.Application.Interfaces.Security;
using SecureWallet.Domain.Entities;

namespace SecureWallet.Infrastructure.Security;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtTokenService> _logger;

    public JwtTokenService(IConfiguration configuration, ILogger<JwtTokenService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public string GenerateAccessToken(User user, bool securitySetupRequired = false)
    {
        JwtSecurityTokenHandler tokenHandler = new();
        SecurityToken securityToken = tokenHandler.CreateToken(BuildTokenDescriptor(user, securitySetupRequired));
        return tokenHandler.WriteToken(securityToken);
    }

    public DateTime GetAccessTokenExpiresAtUtc()
    {
        return DateTime.UtcNow.AddMinutes(GetAccessTokenExpirationMinutes());
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredAccessToken(string expiredAccessToken)
    {
        if (string.IsNullOrWhiteSpace(expiredAccessToken))
        {
            return null;
        }

        JwtSecurityTokenHandler tokenHandler = new();

        TokenValidationParameters validationParameters = BuildValidationParameters();
        validationParameters.ValidateLifetime = false;

        try
        {
            ClaimsPrincipal principal = tokenHandler.ValidateToken(expiredAccessToken, validationParameters, out SecurityToken validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }

    private SecurityTokenDescriptor BuildTokenDescriptor(User user, bool securitySetupRequired)
    {
        List<Claim> claims = new()
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(AuthClaimNames.SecuritySetupRequired, securitySetupRequired.ToString().ToLowerInvariant())
        };

        if (!string.IsNullOrWhiteSpace(user.Role?.Name))
        {
            claims.Add(new Claim(ClaimTypes.Role, user.Role.Name));
        }

        SymmetricSecurityKey signingKey = new(Encoding.UTF8.GetBytes(GetJwtKey()));
        SigningCredentials signingCredentials = new(signingKey, SecurityAlgorithms.HmacSha256);

        return new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = GetAccessTokenExpiresAtUtc(),
            Issuer = GetJwtIssuer(),
            Audience = GetJwtAudience(),
            SigningCredentials = signingCredentials
        };
    }

    private TokenValidationParameters BuildValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = GetJwtIssuer(),
            ValidateAudience = true,
            ValidAudience = GetJwtAudience(),
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetJwtKey())),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    }

    private int GetAccessTokenExpirationMinutes()
    {
        string expirationMinutesValue = _configuration["Jwt:AccessTokenExpirationMinutes"]
            ?? throw CreateConfigurationException("Не е намерена настройката Jwt:AccessTokenExpirationMinutes.");

        if (!int.TryParse(expirationMinutesValue, out int expirationMinutes))
        {
            throw CreateConfigurationException("Стойността на Jwt:AccessTokenExpirationMinutes е невалидна.");
        }

        return expirationMinutes;
    }

    private string GetJwtKey()
    {
        return _configuration["Jwt:Key"]
            ?? throw CreateConfigurationException("Не е намерена настройката Jwt:Key.");
    }

    private string GetJwtIssuer()
    {
        return _configuration["Jwt:Issuer"]
            ?? throw CreateConfigurationException("Не е намерена настройката Jwt:Issuer.");
    }

    private string GetJwtAudience()
    {
        return _configuration["Jwt:Audience"]
            ?? throw CreateConfigurationException("Не е намерена настройката Jwt:Audience.");
    }

    private InvalidOperationException CreateConfigurationException(string details)
    {
        _logger.LogError("Грешка в JWT конфигурацията: {Details}", details);
        return new InvalidOperationException("Услугата временно не е достъпна. Опитай по-късно.");
    }
}
