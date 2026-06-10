namespace SecureWallet.Application.Features.Auth.Commands.Refresh;

public class RefreshSessionCommand
{
    public string ExpiredAccessToken { get; set; } = string.Empty;

    public string TotpCode { get; set; } = string.Empty;
}
