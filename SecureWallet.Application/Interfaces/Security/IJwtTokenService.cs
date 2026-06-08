using SecureWallet.Domain.Entities;

namespace SecureWallet.Application.Interfaces.Security;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user, bool securitySetupRequired = false);

    DateTime GetAccessTokenExpiresAtUtc();

    string GenerateRefreshToken();

    DateTime GetRefreshTokenExpiresAtUtc();
}
