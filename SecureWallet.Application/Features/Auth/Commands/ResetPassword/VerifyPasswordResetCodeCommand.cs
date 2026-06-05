namespace SecureWallet.Application.Features.Auth.Commands.ResetPassword;

public class VerifyPasswordResetCodeCommand
{
    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;
}
