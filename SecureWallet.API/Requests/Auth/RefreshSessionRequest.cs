namespace SecureWallet.API.Requests.Auth;

public class RefreshSessionRequest
{
    public Guid UserId { get; set; }

    public string RefreshToken { get; set; } = string.Empty;
}
