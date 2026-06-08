namespace SecureWallet.Application.Features.Auth.Commands.Refresh;

public class RefreshSessionCommand
{
    public Guid UserId { get; set; }

    public string RefreshToken { get; set; } = string.Empty;
}
