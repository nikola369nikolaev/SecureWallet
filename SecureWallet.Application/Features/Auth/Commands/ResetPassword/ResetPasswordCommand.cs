namespace SecureWallet.Application.Features.Auth.Commands.ResetPassword;

public class ResetPasswordCommand
{
    public string ResetSessionToken { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;
}
