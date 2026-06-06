namespace SecureWallet.API.Requests.Auth;

public class ResendEmailVerificationCodeRequest
{
    public string Email { get; set; } = string.Empty;
}
