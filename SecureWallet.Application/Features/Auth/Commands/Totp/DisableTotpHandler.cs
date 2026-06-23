using FluentValidation;
using SecureWallet.Application.Features.Auth.DTOs;
using SecureWallet.Application.Interfaces.Repositories;
using SecureWallet.Application.Interfaces.Security;
using SecureWallet.Application.Validation;
using SecureWallet.Domain.Entities;

namespace SecureWallet.Application.Features.Auth.Commands.Totp;

public class DisableTotpHandler
{
    private readonly IUserRepository _userRepository;
    private readonly ITotpService _totpService;
    private readonly ITotpSecretProtector _totpSecretProtector;
    private readonly IValidator<DisableTotpCommand> _validator;

    public DisableTotpHandler(
        IUserRepository userRepository,
        ITotpService totpService,
        ITotpSecretProtector totpSecretProtector,
        IValidator<DisableTotpCommand> validator)
    {
        _userRepository = userRepository;
        _totpService = totpService;
        _totpSecretProtector = totpSecretProtector;
        _validator = validator;
    }

    public async Task<TotpVerificationResultDto> Handle(DisableTotpCommand command, CancellationToken cancellationToken = default)
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

        string activeSecret = _totpSecretProtector.Unprotect(user.TotpSecret);
        bool isCodeValid = _totpService.VerifyCode(activeSecret, command.Code);
        if (!isCodeValid)
        {
            throw new InvalidOperationException("Временният код е грешен.");
        }

        user.TotpSecret = null;
        user.PendingTotpSecret = null;
        user.TwoFactorEnabled = false;
        user.UpdatedAtUtc = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user, cancellationToken);

        return new TotpVerificationResultDto
        {
            Message = "Двуфакторната защита е изключена.",
            TwoFactorEnabled = false
        };
    }
}
