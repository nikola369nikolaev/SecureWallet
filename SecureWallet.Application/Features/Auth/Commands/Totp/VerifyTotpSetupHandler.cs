using SecureWallet.Application.Features.Auth.DTOs;
using SecureWallet.Application.Features.Auth.Validators;
using SecureWallet.Application.Interfaces.Repositories;
using SecureWallet.Application.Interfaces.Security;
using SecureWallet.Domain.Entities;

namespace SecureWallet.Application.Features.Auth.Commands.Totp;

public class VerifyTotpSetupHandler
{
    private readonly IUserRepository _userRepository;
    private readonly ITotpService _totpService;
    private readonly IJwtTokenService _jwtTokenService;

    public VerifyTotpSetupHandler(
        IUserRepository userRepository,
        ITotpService totpService,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _totpService = totpService;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<TotpVerificationResultDto> Handle(VerifyTotpSetupCommand command, CancellationToken cancellationToken = default)
    {
        AuthInputValidator.ValidateRequiredField(command.Code, "Кодът от authenticator приложението");
        AuthInputValidator.ValidateNoLeadingOrTrailingWhitespace(command.Code, "Кодът от authenticator приложението");

        User? user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            throw new InvalidOperationException("Потребителят не беше намерен.");
        }

        if (string.IsNullOrWhiteSpace(user.PendingTotpSecret))
        {
            throw new InvalidOperationException("Няма активна TOTP настройка за този акаунт.");
        }

        bool isCodeValid = _totpService.VerifyCode(user.PendingTotpSecret, command.Code);
        if (!isCodeValid)
        {
            throw new InvalidOperationException("Кодът от authenticator приложението е грешен.");
        }

        user.TotpSecret = user.PendingTotpSecret;
        user.PendingTotpSecret = null;
        user.TwoFactorEnabled = true;
        user.UpdatedAtUtc = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user, cancellationToken);

        string accessToken = _jwtTokenService.GenerateAccessToken(user);

        return new TotpVerificationResultDto
        {
            Message = "Двуфакторната защита беше включена успешно.",
            TwoFactorEnabled = true,
            AccessToken = accessToken,
            ExpiresAtUtc = _jwtTokenService.GetAccessTokenExpiresAtUtc(),
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role?.Name ?? string.Empty,
            IsEmailVerified = user.IsEmailVerified,
            SecuritySetupRequired = false
        };
    }
}
