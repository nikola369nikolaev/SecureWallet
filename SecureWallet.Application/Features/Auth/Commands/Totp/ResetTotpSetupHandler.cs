using FluentValidation;
using SecureWallet.Application.Features.Auth.DTOs;
using SecureWallet.Application.Interfaces.Repositories;
using SecureWallet.Application.Interfaces.Security;
using SecureWallet.Application.Validation;
using SecureWallet.Domain.Entities;

namespace SecureWallet.Application.Features.Auth.Commands.Totp;

public class ResetTotpSetupHandler
{
    private const string IssuerName = "SecureWallet";

    private readonly IUserRepository _userRepository;
    private readonly ITotpService _totpService;
    private readonly IQrCodeService _qrCodeService;
    private readonly IValidator<ResetTotpSetupCommand> _validator;

    public ResetTotpSetupHandler(
        IUserRepository userRepository,
        ITotpService totpService,
        IQrCodeService qrCodeService,
        IValidator<ResetTotpSetupCommand> validator)
    {
        _userRepository = userRepository;
        _totpService = totpService;
        _qrCodeService = qrCodeService;
        _validator = validator;
    }

    public async Task<TotpSetupDto> Handle(ResetTotpSetupCommand command, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowInvalidOperationAsync(command, cancellationToken);

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
            throw new InvalidOperationException("Временният код е грешен.");
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
            Message = "Старият временен код остава активен, докато не потвърдиш новия код.",
            ManualEntryKey = user.PendingTotpSecret,
            SetupCodeUri = setupCodeUri,
            QrCodeImageDataUri = qrCodeImageDataUri
        };
    }
}
