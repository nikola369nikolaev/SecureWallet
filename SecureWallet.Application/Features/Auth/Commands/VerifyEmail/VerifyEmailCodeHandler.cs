using FluentValidation;
using SecureWallet.Application.Features.Auth.DTOs;
using SecureWallet.Application.Interfaces.Repositories;
using SecureWallet.Application.Interfaces.Security;
using SecureWallet.Application.Validation;
using SecureWallet.Domain.Entities;

namespace SecureWallet.Application.Features.Auth.Commands.VerifyEmail;

public class VerifyEmailCodeHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly AuthSessionIssuer _authSessionIssuer;
    private readonly IValidator<VerifyEmailCodeCommand> _validator;

    public VerifyEmailCodeHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        AuthSessionIssuer authSessionIssuer,
        IValidator<VerifyEmailCodeCommand> validator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _authSessionIssuer = authSessionIssuer;
        _validator = validator;
    }

    public async Task<EmailVerificationResultDto> Handle(VerifyEmailCodeCommand command, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowInvalidOperationAsync(command, cancellationToken);

        User? user = await _userRepository.GetByEmailAsync(command.Email, cancellationToken);
        if (user is null)
        {
            throw new InvalidOperationException("Не беше намерен акаунт с този имейл.");
        }

        if (user.IsEmailVerified)
        {
            throw new InvalidOperationException("Имейлът вече е потвърден.");
        }

        if (string.IsNullOrWhiteSpace(user.EmailVerificationCodeHash) || user.EmailVerificationCodeExpiresAtUtc is null)
        {
            throw new InvalidOperationException("Няма активен код за потвърждение на имейла.");
        }

        if (user.EmailVerificationCodeExpiresAtUtc.Value <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("Кодът за потвърждение е изтекъл. Поискай нов код и опитай отново.");
        }

        if (!_passwordHasher.Verify(command.Code, user.EmailVerificationCodeHash))
        {
            throw new InvalidOperationException("Кодът за потвърждение е грешен.");
        }

        user.IsEmailVerified = true;
        user.EmailVerificationCodeHash = null;
        user.EmailVerificationCodeExpiresAtUtc = null;
        user.UpdatedAtUtc = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user, cancellationToken);
        AuthSessionTokens tokens = await _authSessionIssuer.IssueAsync(user, true, cancellationToken);

        return new EmailVerificationResultDto
        {
            Message = "Имейлът е потвърден. Продължи с настройката на временния код.",
            AccessToken = tokens.AccessToken,
            ExpiresAtUtc = tokens.AccessTokenExpiresAtUtc,
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role?.Name ?? string.Empty,
            TwoFactorEnabled = user.TwoFactorEnabled,
            IsEmailVerified = true,
            SecuritySetupRequired = true
        };
    }
}
