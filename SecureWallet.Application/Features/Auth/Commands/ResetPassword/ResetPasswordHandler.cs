using SecureWallet.Application.Features.Auth.Validators;
using SecureWallet.Application.Interfaces.Repositories;
using SecureWallet.Application.Interfaces.Security;
using SecureWallet.Domain.Entities;

namespace SecureWallet.Application.Features.Auth.Commands.ResetPassword;

public class ResetPasswordHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public ResetPasswordHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task Handle(ResetPasswordCommand command, CancellationToken cancellationToken = default)
    {
        AuthInputValidator.ValidateRequiredField(command.ResetSessionToken, "Токенът за смяна на паролата");
        AuthInputValidator.ValidateNoLeadingOrTrailingWhitespace(command.ResetSessionToken, "Токенът за смяна на паролата");

        IReadOnlyCollection<string> passwordErrors = PasswordValidator.Validate(command.NewPassword);

        if (passwordErrors.Count > 0)
        {
            throw new InvalidOperationException(passwordErrors.First());
        }

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
        user.UpdatedAtUtc = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user, cancellationToken);
    }
}
