namespace SecureWallet.Application.Features.Auth.Commands.ResetPassword;

public class RequestPasswordResetCodeCommand
{
    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;
}
