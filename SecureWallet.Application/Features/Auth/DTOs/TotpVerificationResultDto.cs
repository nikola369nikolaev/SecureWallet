namespace SecureWallet.Application.Features.Auth.DTOs;

public class TotpVerificationResultDto
{
    public string Message { get; set; } = string.Empty;

    public bool TwoFactorEnabled { get; set; }
}
