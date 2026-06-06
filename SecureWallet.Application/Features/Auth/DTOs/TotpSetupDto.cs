namespace SecureWallet.Application.Features.Auth.DTOs;

public class TotpSetupDto
{
    public bool IsAlreadyEnabled { get; set; }

    public bool CanShowQrCode { get; set; }

    public string Message { get; set; } = string.Empty;

    public string ManualEntryKey { get; set; } = string.Empty;

    public string SetupCodeUri { get; set; } = string.Empty;

    public string QrCodeImageDataUri { get; set; } = string.Empty;
}
