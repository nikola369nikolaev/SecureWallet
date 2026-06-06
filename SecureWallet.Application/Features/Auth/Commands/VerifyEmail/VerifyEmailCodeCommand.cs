namespace SecureWallet.Application.Features.Auth.Commands.VerifyEmail;

public class VerifyEmailCodeCommand
{
    public string Email { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;
}
