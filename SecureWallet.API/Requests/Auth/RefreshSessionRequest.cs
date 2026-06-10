namespace SecureWallet.API.Requests.Auth;

public class RefreshSessionRequest
{
    public string ExpiredAccessToken { get; set; } = string.Empty;

    public string TotpCode { get; set; } = string.Empty;
}
