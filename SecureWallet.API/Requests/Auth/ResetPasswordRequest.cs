namespace SecureWallet.API.Requests.Auth;

public class ResetPasswordRequest
{
    public string ResetSessionToken { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;
}
