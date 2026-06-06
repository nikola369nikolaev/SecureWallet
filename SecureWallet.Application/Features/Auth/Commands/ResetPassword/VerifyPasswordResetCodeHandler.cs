using SecureWallet.Application.Features.Auth.DTOs;
using SecureWallet.Application.Features.Auth.Validators;
using SecureWallet.Application.Interfaces.Repositories;
using SecureWallet.Application.Interfaces.Security;
using SecureWallet.Domain.Entities;

namespace SecureWallet.Application.Features.Auth.Commands.ResetPassword;

public class VerifyPasswordResetCodeHandler
{
    private static readonly TimeSpan PasswordResetCodeLockoutDuration = TimeSpan.FromMinutes(15);

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public VerifyPasswordResetCodeHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<PasswordResetCodeVerificationResultDto> Handle(VerifyPasswordResetCodeCommand command, CancellationToken cancellationToken = default)
    {
        AuthInputValidator.ValidateEmail(command.Email);
        AuthInputValidator.ValidatePhoneNumber(command.PhoneNumber);
        AuthInputValidator.ValidateRequiredField(command.Code, "Кодът за потвърждение");
        AuthInputValidator.ValidateNoLeadingOrTrailingWhitespace(command.Code, "Кодът за потвърждение");

        User? user = await _userRepository.GetByEmailAndPhoneNumberAsync(command.Email, command.PhoneNumber, cancellationToken);

        if (user is null)
        {
            throw new InvalidOperationException("Не беше намерен акаунт с този имейл и телефонен номер.");
        }

        if (user.PasswordResetCodeLockoutEndUtc is not null && user.PasswordResetCodeLockoutEndUtc.Value > DateTime.UtcNow)
        {
            throw new InvalidOperationException("Смяната на паролата е заключена за 15 минути след 3 грешни кода.");
        }

        if (string.IsNullOrWhiteSpace(user.PasswordResetCodeHash) || user.PasswordResetCodeExpiresAtUtc is null)
        {
            throw new InvalidOperationException("Няма активен код за смяна на паролата за този акаунт.");
        }

        if (user.PasswordResetCodeExpiresAtUtc.Value <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("SMS кодът е изтекъл. Поискай нов код.");
        }

        if (!_passwordHasher.Verify(command.Code, user.PasswordResetCodeHash))
        {
            user.FailedPasswordResetCodeAttempts += 1;
            user.UpdatedAtUtc = DateTime.UtcNow;

            if (user.FailedPasswordResetCodeAttempts >= 3)
            {
                user.FailedPasswordResetCodeAttempts = 0;
                user.PasswordResetCodeHash = null;
                user.PasswordResetCodeExpiresAtUtc = null;
                user.PasswordResetCodeLockoutEndUtc = DateTime.UtcNow.Add(PasswordResetCodeLockoutDuration);

                await _userRepository.UpdateAsync(user, cancellationToken);
                throw new InvalidOperationException("Твърде много грешни кодове. Смяната на паролата е заключена за 15 минути.");
            }

            await _userRepository.UpdateAsync(user, cancellationToken);
            throw new InvalidOperationException("SMS кодът е грешен.");
        }

        string resetSessionToken = Guid.NewGuid().ToString("N");

        user.FailedPasswordResetCodeAttempts = 0;
        user.PasswordResetCodeLockoutEndUtc = null;
        user.PasswordResetCodeHash = null;
        user.PasswordResetCodeExpiresAtUtc = null;
        user.PasswordResetSessionToken = resetSessionToken;
        user.PasswordResetSessionExpiresAtUtc = DateTime.UtcNow.AddMinutes(10);
        user.UpdatedAtUtc = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user, cancellationToken);

        return new PasswordResetCodeVerificationResultDto
        {
            Message = "SMS потвърждението беше успешно.",
            ResetSessionToken = resetSessionToken,
            Email = user.Email
        };
    }
}
