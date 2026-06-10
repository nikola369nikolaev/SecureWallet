using FluentValidation;
using SecureWallet.Application.Features.Auth.DTOs;
using SecureWallet.Application.Interfaces.Repositories;
using SecureWallet.Application.Interfaces.Security;
using SecureWallet.Application.Validation;
using SecureWallet.Domain.Entities;

namespace SecureWallet.Application.Features.Auth.Commands.ResetPassword;

public class ResetPasswordHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly AuthSessionIssuer _authSessionIssuer;
    private readonly IValidator<ResetPasswordCommand> _validator;

    public ResetPasswordHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        AuthSessionIssuer authSessionIssuer,
        IValidator<ResetPasswordCommand> validator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _authSessionIssuer = authSessionIssuer;
        _validator = validator;
    }

    public async Task<PasswordResetCompletionResultDto> Handle(ResetPasswordCommand command, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowInvalidOperationAsync(command, cancellationToken);

        User? user = await _userRepository.GetByPasswordResetSessionTokenAsync(command.ResetSessionToken, cancellationToken);
        if (user is null)
        {
            throw new InvalidOperationException("Сесията за смяна на паролата е невалидна.");
        }

        if (user.PasswordResetSessionExpiresAtUtc is null || user.PasswordResetSessionExpiresAtUtc.Value <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("Сесията за смяна на паролата е изтекла. Започни процеса отново.");
        }

        user.Password = _passwordHasher.Hash(command.NewPassword);
        user.FailedLoginAttempts = 0;
        user.CurrentCaptchaCode = null;
        user.LockoutEndUtc = null;
        user.PasswordResetCodeHash = null;
        user.PasswordResetCodeExpiresAtUtc = null;
        user.PasswordResetSessionToken = null;
        user.PasswordResetSessionExpiresAtUtc = null;
        user.TwoFactorEnabled = false;
        user.TotpSecret = null;
        user.PendingTotpSecret = null;
        user.UpdatedAtUtc = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user, cancellationToken);
        AuthSessionTokens tokens = await _authSessionIssuer.IssueAsync(user, true, cancellationToken);

        return new PasswordResetCompletionResultDto
        {
            Message = "Паролата е сменена. Продължи с новата настройка на временния код.",
            AccessToken = tokens.AccessToken,
            ExpiresAtUtc = tokens.AccessTokenExpiresAtUtc,
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role?.Name ?? string.Empty,
            TwoFactorEnabled = false,
            IsEmailVerified = user.IsEmailVerified,
            SecuritySetupRequired = true
        };
    }
}
