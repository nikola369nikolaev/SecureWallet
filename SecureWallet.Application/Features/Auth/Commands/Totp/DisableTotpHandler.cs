using SecureWallet.Application.Features.Auth.DTOs;
using SecureWallet.Application.Features.Auth.Validators;
using SecureWallet.Application.Interfaces.Repositories;
using SecureWallet.Application.Interfaces.Security;
using SecureWallet.Domain.Entities;

namespace SecureWallet.Application.Features.Auth.Commands.Totp;

public class DisableTotpHandler
{
    private readonly IUserRepository _userRepository;
    private readonly ITotpService _totpService;

    public DisableTotpHandler(IUserRepository userRepository, ITotpService totpService)
    {
        _userRepository = userRepository;
        _totpService = totpService;
    }

    public async Task<TotpVerificationResultDto> Handle(DisableTotpCommand command, CancellationToken cancellationToken = default)
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

        user.TotpSecret = null;
        user.PendingTotpSecret = null;
        user.TwoFactorEnabled = false;
        user.UpdatedAtUtc = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user, cancellationToken);

        return new TotpVerificationResultDto
        {
            Message = "Двуфакторната защита беше изключена успешно.",
            TwoFactorEnabled = false
        };
    }
}
