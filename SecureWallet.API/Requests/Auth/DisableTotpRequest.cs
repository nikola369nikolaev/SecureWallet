namespace SecureWallet.API.Requests.Auth;

public class DisableTotpRequest
{
    public string Code { get; set; } = string.Empty;
}
