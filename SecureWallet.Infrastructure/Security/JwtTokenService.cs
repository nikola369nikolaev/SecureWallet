using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SecureWallet.Application.Features.Auth;
using SecureWallet.Application.Interfaces.Security;
using SecureWallet.Domain.Entities;

namespace SecureWallet.Infrastructure.Security;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
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
            ?? throw new InvalidOperationException("Не е намерена настройка за живота на JWT сесията.");

        if (!int.TryParse(expirationMinutesValue, out int expirationMinutes))
        {
            throw new InvalidOperationException("Стойността за живота на JWT сесията е невалидна.");
        }

        return expirationMinutes;
    }

    private string GetJwtKey()
    {
        return _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Не е намерен ключът за JWT.");
    }

    private string GetJwtIssuer()
    {
        return _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Не е намерен issuer-ът за JWT.");
    }

    private string GetJwtAudience()
    {
        return _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("Не е намерена audience стойността за JWT.");
    }
}
