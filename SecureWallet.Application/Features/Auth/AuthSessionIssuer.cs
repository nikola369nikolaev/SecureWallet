using SecureWallet.Application.Interfaces.Security;
using SecureWallet.Domain.Entities;

namespace SecureWallet.Application.Features.Auth;

public class AuthSessionIssuer
{
    private readonly IJwtTokenService _jwtTokenService;

    public AuthSessionIssuer(IJwtTokenService jwtTokenService)
    {
        _jwtTokenService = jwtTokenService;
    }

    public Task<AuthSessionTokens> IssueAsync(
        User user,
        bool securitySetupRequired,
        CancellationToken cancellationToken = default)
    {
        AuthSessionTokens tokens = new()
        {
            AccessToken = _jwtTokenService.GenerateAccessToken(user, securitySetupRequired),
            AccessTokenExpiresAtUtc = _jwtTokenService.GetAccessTokenExpiresAtUtc()
        };

        return Task.FromResult(tokens);
    }
}
