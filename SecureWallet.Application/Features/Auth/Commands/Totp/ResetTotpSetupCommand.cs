namespace SecureWallet.Application.Features.Auth.Commands.Totp;

public class ResetTotpSetupCommand
{
    public Guid UserId { get; set; }

    public string Code { get; set; } = string.Empty;
}
