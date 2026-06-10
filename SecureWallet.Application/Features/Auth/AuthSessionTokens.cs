namespace SecureWallet.Application.Features.Auth;

public class AuthSessionTokens
{
    public string AccessToken { get; set; } = string.Empty;

    public DateTime AccessTokenExpiresAtUtc { get; set; }
}
