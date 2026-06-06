namespace SecureWallet.API.Requests.Auth;

public class VerifyTotpSetupRequest
{
    public string Code { get; set; } = string.Empty;
}
