namespace SecureWallet.API.Requests.Auth;

public class VerifyEmailCodeRequest
{
    public string Email { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;
}
