namespace SecureWallet.Application.Features.Auth.Commands.VerifyEmail;

public class ResendEmailVerificationCodeCommand
{
    public string Email { get; set; } = string.Empty;
}
