using SecureWallet.Application.Features.Auth.DTOs;
using SecureWallet.Application.Features.Auth.Validators;
using SecureWallet.Application.Interfaces.Repositories;
using SecureWallet.Application.Interfaces.Security;
using SecureWallet.Domain.Entities;

namespace SecureWallet.Application.Features.Auth.Commands.Totp;

public class ResetTotpSetupHandler
{
    private const string IssuerName = "SecureWallet";

    private readonly IUserRepository _userRepository;
    private readonly ITotpService _totpService;
    private readonly IQrCodeService _qrCodeService;

    public ResetTotpSetupHandler(
        IUserRepository userRepository,
        ITotpService totpService,
        IQrCodeService qrCodeService)
    {
        _userRepository = userRepository;
        _totpService = totpService;
        _qrCodeService = qrCodeService;
    }

    public async Task<TotpSetupDto> Handle(ResetTotpSetupCommand command, CancellationToken cancellationToken = default)
    {
        AuthInputValidator.ValidateRequiredField(command.Code, "Кодът от authenticator приложението");
        AuthInputValidator.ValidateNoLeadingOrTrailingWhitespace(command.Code, "Кодът от authenticator приложението");

        User? user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            throw new InvalidOperationException("Потребителят не беше намерен.");
        }

        if (!user.TwoFactorEnabled || string.IsNullOrWhiteSpace(user.TotpSecret))
        {
            throw new InvalidOperationException("Двуфакторната защита не е включена за този акаунт.");
        }

        bool isCodeValid = _totpService.VerifyCode(user.TotpSecret, command.Code);
        if (!isCodeValid)
        {
            throw new InvalidOperationException("Кодът от authenticator приложението е грешен.");
        }

        user.PendingTotpSecret = _totpService.GenerateSecret();
        user.UpdatedAtUtc = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user, cancellationToken);

        string setupCodeUri = _totpService.BuildSetupCodeUri(IssuerName, user.Email, user.PendingTotpSecret);
        string qrCodeImageDataUri = _qrCodeService.GenerateSvgDataUri(setupCodeUri);

        return new TotpSetupDto
        {
            IsAlreadyEnabled = false,
            CanShowQrCode = true,
            Message = "Старият authenticator остава активен, докато не потвърдиш новия код.",
            ManualEntryKey = user.PendingTotpSecret,
            SetupCodeUri = setupCodeUri,
            QrCodeImageDataUri = qrCodeImageDataUri
        };
    }
}
