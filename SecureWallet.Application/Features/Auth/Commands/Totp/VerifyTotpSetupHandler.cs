using FluentValidation;
using SecureWallet.Application.Features.Auth.DTOs;
using SecureWallet.Application.Interfaces.Repositories;
using SecureWallet.Application.Interfaces.Security;
using SecureWallet.Application.Validation;
using SecureWallet.Domain.Entities;

namespace SecureWallet.Application.Features.Auth.Commands.Totp;

public class VerifyTotpSetupHandler
{
    private readonly IUserRepository _userRepository;
    private readonly ITotpService _totpService;
    private readonly AuthSessionIssuer _authSessionIssuer;
    private readonly IValidator<VerifyTotpSetupCommand> _validator;

    public VerifyTotpSetupHandler(
        IUserRepository userRepository,
        ITotpService totpService,
        AuthSessionIssuer authSessionIssuer,
        IValidator<VerifyTotpSetupCommand> validator)
    {
        _userRepository = userRepository;
        _totpService = totpService;
        _authSessionIssuer = authSessionIssuer;
        _validator = validator;
    }

    public async Task<TotpVerificationResultDto> Handle(VerifyTotpSetupCommand command, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowInvalidOperationAsync(command, cancellationToken);

        User? user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            throw new InvalidOperationException("Потребителят не беше намерен.");
        }

        if (string.IsNullOrWhiteSpace(user.PendingTotpSecret))
        {
            throw new InvalidOperationException("Няма активна настройка на временен код за този акаунт.");
        }

        bool isCodeValid = _totpService.VerifyCode(user.PendingTotpSecret, command.Code);
        if (!isCodeValid)
        {
            throw new InvalidOperationException("Временният код е грешен.");
        }

        user.TotpSecret = user.PendingTotpSecret;
        user.PendingTotpSecret = null;
        user.TwoFactorEnabled = true;
        user.UpdatedAtUtc = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user, cancellationToken);
        AuthSessionTokens tokens = await _authSessionIssuer.IssueAsync(user, false, cancellationToken);

        return new TotpVerificationResultDto
        {
            Message = "Двуфакторната защита е включена.",
            TwoFactorEnabled = true,
            AccessToken = tokens.AccessToken,
            ExpiresAtUtc = tokens.AccessTokenExpiresAtUtc,
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role?.Name ?? string.Empty,
            IsEmailVerified = user.IsEmailVerified,
            SecuritySetupRequired = false
        };
    }
}
