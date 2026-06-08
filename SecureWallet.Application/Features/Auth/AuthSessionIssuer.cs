using SecureWallet.Application.Interfaces.Repositories;
using SecureWallet.Application.Interfaces.Security;
using SecureWallet.Domain.Entities;

namespace SecureWallet.Application.Features.Auth;

public class AuthSessionIssuer
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthSessionIssuer(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthSessionTokens> IssueAsync(
        User user,
        bool securitySetupRequired,
        CancellationToken cancellationToken = default)
    {
        string refreshToken = _jwtTokenService.GenerateRefreshToken();
        DateTime refreshTokenExpiresAtUtc = _jwtTokenService.GetRefreshTokenExpiresAtUtc();

        user.RefreshTokenHash = _passwordHasher.Hash(refreshToken);
        user.RefreshTokenExpiresAtUtc = refreshTokenExpiresAtUtc;
        user.UpdatedAtUtc = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user, cancellationToken);

        return new AuthSessionTokens
        {
            AccessToken = _jwtTokenService.GenerateAccessToken(user, securitySetupRequired),
            AccessTokenExpiresAtUtc = _jwtTokenService.GetAccessTokenExpiresAtUtc(),
            RefreshToken = refreshToken,
            RefreshTokenExpiresAtUtc = refreshTokenExpiresAtUtc
        };
    }

    public bool IsRefreshTokenValid(User user, string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken) ||
            string.IsNullOrWhiteSpace(user.RefreshTokenHash) ||
            user.RefreshTokenExpiresAtUtc is null)
        {
            return false;
        }

        if (user.RefreshTokenExpiresAtUtc.Value <= DateTime.UtcNow)
        {
            return false;
        }

        return _passwordHasher.Verify(refreshToken, user.RefreshTokenHash);
    }
}
