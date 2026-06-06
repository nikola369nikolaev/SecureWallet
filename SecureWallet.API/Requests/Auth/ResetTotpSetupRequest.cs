namespace SecureWallet.API.Requests.Auth;

public class ResetTotpSetupRequest
{
    public string Code { get; set; } = string.Empty;
}
