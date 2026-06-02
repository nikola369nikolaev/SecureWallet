using SecureWallet.Domain.Entities;

namespace SecureWallet.Application.Interfaces.Security;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
}
