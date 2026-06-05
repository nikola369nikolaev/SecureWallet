namespace SecureWallet.API.Requests.Auth;

public class RequestPasswordResetCodeRequest
{
    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;
}
