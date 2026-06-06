namespace SecureWallet.Application.Features.Auth.Commands.Totp;

public class DisableTotpCommand
{
    public Guid UserId { get; set; }

    public string Code { get; set; } = string.Empty;
}
