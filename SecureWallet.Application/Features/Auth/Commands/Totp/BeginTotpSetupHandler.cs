using SecureWallet.Application.Features.Auth.DTOs;
using SecureWallet.Application.Interfaces.Repositories;
using SecureWallet.Application.Interfaces.Security;
using SecureWallet.Domain.Entities;

namespace SecureWallet.Application.Features.Auth.Commands.Totp;

public class BeginTotpSetupHandler
{
    private const string IssuerName = "SecureWallet";

    private readonly IUserRepository _userRepository;
    private readonly IQrCodeService _qrCodeService;
    private readonly ITotpService _totpService;
    private readonly ITotpSecretProtector _totpSecretProtector;

    public BeginTotpSetupHandler(
        IUserRepository userRepository,
        ITotpService totpService,
        IQrCodeService qrCodeService,
        ITotpSecretProtector totpSecretProtector)
    {
        _userRepository = userRepository;
        _totpService = totpService;
        _qrCodeService = qrCodeService;
        _totpSecretProtector = totpSecretProtector;
    }

    public async Task<TotpSetupDto> Handle(Guid userId, CancellationToken cancellationToken = default)
    {
        User? user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new InvalidOperationException("Потребителят не беше намерен.");
        }

        if (user.TwoFactorEnabled && !string.IsNullOrWhiteSpace(user.TotpSecret))
        {
            return new TotpSetupDto
            {
                IsAlreadyEnabled = true,
                CanShowQrCode = false,
                Message = "Двуфакторната защита вече е включена за този акаунт."
            };
        }

        string pendingSecret;
        if (string.IsNullOrWhiteSpace(user.PendingTotpSecret))
        {
            pendingSecret = _totpService.GenerateSecret();
            user.PendingTotpSecret = _totpSecretProtector.Protect(pendingSecret);
            user.UpdatedAtUtc = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user, cancellationToken);
        }
        else
        {
            pendingSecret = _totpSecretProtector.Unprotect(user.PendingTotpSecret);
        }

        string accountName = user.Email;
        string setupCodeUri = _totpService.BuildSetupCodeUri(IssuerName, accountName, pendingSecret);
        string qrCodeImageDataUri = _qrCodeService.GenerateSvgDataUri(setupCodeUri);

        return new TotpSetupDto
        {
            IsAlreadyEnabled = false,
            CanShowQrCode = true,
            Message = "Сканирай QR кода или въведи ключа ръчно в приложението за временни кодове.",
            ManualEntryKey = pendingSecret,
            SetupCodeUri = setupCodeUri,
            QrCodeImageDataUri = qrCodeImageDataUri
        };
    }
}
